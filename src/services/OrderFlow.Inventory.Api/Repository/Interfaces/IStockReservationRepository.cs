using OrderFlow.Inventory.Api.Entities;
using OrderFlow.Inventory.Api.Repository.Base;

namespace OrderFlow.Inventory.Api.Repository.Interfaces;

public interface IStockReservationRepository : IOrderFlowBaseRepository<StockReservation>
{
    Task<StockReservation?> GetByOrderIdAsync(Guid orderId);
}
