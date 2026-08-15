using Microsoft.EntityFrameworkCore;
using OrderFlow.Orders.Api.Entities;
using OrderFlow.Orders.Api.Repository.Base;
using OrderFlow.Orders.Api.Repository.DbContext;
using OrderFlow.Orders.Api.Repository.Interfaces;

namespace OrderFlow.Orders.Api.Repository.Implementations;

public class OrderRepository(OrdersDbContext dbContext)
    : OrderFlowMainDbRepository<Order>(dbContext), IOrderRepository
{
    public async Task<List<Order>> GetAllOrdered()
    {
        return await db.Orders
            .Include(order => order.Items)
            .OrderByDescending(order => order.DateCreated)
            .ToListAsync();
    }

    public async Task<Order?> GetByIdWithItemsAsync(Guid id)
    {
        return await db.Orders
            .Include(order => order.Items)
            .FirstOrDefaultAsync(order => order.Id == id);
    }
}
