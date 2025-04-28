using ProductsApi.Domain.Entities.Data;
using ProductsApi.Domain.Entities.Mappings;

namespace ProductsApi.Application.Services
{
    public interface IProductService
    {

       public Task<PagedResult<Product>> GetPageAsync(
       int? page,
       int? pageSize,
       CancellationToken ct = default);

       Task<PagedProductResponse> GetPageResponseAsync(
       int? page, int? pageSize, CancellationToken ct = default);
    }
}
