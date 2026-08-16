using OrderFlow.Contracts;
using OrderFlow.Inventory.Api.Entities;
using OrderFlow.Inventory.Api.Repository.DbContext;
using OrderFlow.Inventory.Api.Repository.Interfaces;

namespace OrderFlow.Inventory.Api.Systems;

public class StockSubsystem : IStockSubsystem
{
    private readonly InventoryDbContext _db;
    private readonly IProductRepository _productRepository;
    private readonly IStockReservationRepository _reservationRepository;

    public StockSubsystem(InventoryDbContext db, IProductRepository productRepository, IStockReservationRepository reservationRepository)
    {
        _db = db;
        _productRepository = productRepository;
        _reservationRepository = reservationRepository;
    }

    public async Task<StockReservationResult> ReserveAsync(Guid orderId, IReadOnlyList<OrderLine> items)
    {
        var existing = await _reservationRepository.GetByOrderIdAsync(orderId);
        if (existing is not null)
        {
            // Повторная доставка ReserveStock для уже обработанного заказа — идемпотентный no-op.
            return new StockReservationResult(true, null);
        }

        var productNames = items.Select(item => item.ProductName).Distinct().ToList();
        var products = await _productRepository.GetByNamesAsync(productNames);
        var byName = products.ToDictionary(product => product.Name);

        foreach (var item in items)
        {
            if (!byName.TryGetValue(item.ProductName, out var product) || product.AvailableQuantity < item.Quantity)
            {
                return new StockReservationResult(false, $"Insufficient stock for '{item.ProductName}'.");
            }
        }

        await using var transaction = await _db.Database.BeginTransactionAsync();

        foreach (var item in items)
        {
            byName[item.ProductName].AvailableQuantity -= item.Quantity;
        }
        await _productRepository.UpdateRangeAsync(products);

        await _reservationRepository.AddAsync(new StockReservation
        {
            OrderId = orderId,
            Lines = items.Select(item => new StockReservationLine { ProductName = item.ProductName, Quantity = item.Quantity }).ToList(),
        });

        await transaction.CommitAsync();

        return new StockReservationResult(true, null);
    }

    public async Task ReleaseAsync(Guid orderId)
    {
        var reservation = await _reservationRepository.GetByOrderIdAsync(orderId);
        if (reservation is null || reservation.Status == StockReservationStatus.Released)
        {
            // Резерва не было или он уже освобождён — идемпотентный no-op.
            return;
        }

        var productNames = reservation.Lines.Select(line => line.ProductName).Distinct().ToList();
        var products = await _productRepository.GetByNamesAsync(productNames);
        var byName = products.ToDictionary(product => product.Name);

        await using var transaction = await _db.Database.BeginTransactionAsync();

        foreach (var line in reservation.Lines)
        {
            if (byName.TryGetValue(line.ProductName, out var product))
            {
                product.AvailableQuantity += line.Quantity;
            }
        }
        await _productRepository.UpdateRangeAsync(products);

        reservation.Status = StockReservationStatus.Released;
        await _reservationRepository.UpdateAsync(reservation);

        await transaction.CommitAsync();
    }
}
