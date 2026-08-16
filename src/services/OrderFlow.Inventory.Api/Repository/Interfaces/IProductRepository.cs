using OrderFlow.Inventory.Api.Entities;
using OrderFlow.Inventory.Api.Repository.Base;

namespace OrderFlow.Inventory.Api.Repository.Interfaces;

public interface IProductRepository : IOrderFlowBaseRepository<Product>
{
    Task<List<Product>> GetByNamesAsync(IReadOnlyCollection<string> names);
}
