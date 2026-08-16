using Microsoft.EntityFrameworkCore;
using OrderFlow.Inventory.Api.Entities;
using OrderFlow.Inventory.Api.Repository.Base;
using OrderFlow.Inventory.Api.Repository.DbContext;
using OrderFlow.Inventory.Api.Repository.Interfaces;

namespace OrderFlow.Inventory.Api.Repository.Implementations;

public class ProductRepository(InventoryDbContext dbContext)
    : OrderFlowMainDbRepository<Product>(dbContext), IProductRepository
{
    public async Task<List<Product>> GetByNamesAsync(IReadOnlyCollection<string> names)
    {
        return await db.Products
            .Where(product => names.Contains(product.Name))
            .ToListAsync();
    }
}
