using Microsoft.EntityFrameworkCore;
using OrderFlow.Inventory.Api.Entities;
using OrderFlow.Inventory.Api.Repository.Base;
using OrderFlow.Inventory.Api.Repository.DbContext;
using OrderFlow.Inventory.Api.Repository.Interfaces;

namespace OrderFlow.Inventory.Api.Repository.Implementations;

public class StockReservationRepository(InventoryDbContext dbContext)
    : OrderFlowMainDbRepository<StockReservation>(dbContext), IStockReservationRepository
{
    public async Task<StockReservation?> GetByOrderIdAsync(Guid orderId)
    {
        return await db.StockReservations
            .Include(reservation => reservation.Lines)
            .FirstOrDefaultAsync(reservation => reservation.OrderId == orderId);
    }
}
