using ProductsApi.Domain.Entities.Data;
using ProductsApi.Domain.Entities.Mappings;

namespace ProductsApi.Application.Services
{
    /// <summary>
    /// Defines the contract for product-related operations, including retrieving paginated
    /// product data and working with JSON-based product and attribute files.
    /// </summary>
    public interface IProductService
    {
        /// <summary>
        /// Retrieves a paginated list of products from the data source.
        /// </summary>
        /// <param name="page">The page number to retrieve (optional).</param>
        /// <param name="pageSize">The number of items per page (optional).</param>
        /// <param name="ct">A cancellation token to cancel the operation (optional).</param>
        /// <returns>A <see cref="PagedResult{Product}"/> containing the paginated products.</r
        public Task<PagedResult<Product>> GetPageAsync(
       int? page,
       int? pageSize,
       CancellationToken ct = default);

        /// <summary>
        /// Retrieves a paginated list of products along with mapped attributes corresponding with the attributes meta.
        /// </summary>
        /// <param name="page">The page number to retrieve (optional).</param>
        /// <param name="pageSize">The number of items per page (optional).</param>
        /// <param name="ct">A cancellation token to cancel the operation (optional).</param>
        /// <returns>A <see cref="PagedProductResponse"/> containing the paginated products an
        public Task<PagedProductResponse> GetPageResponseAsync(
       int? page, int? pageSize, CancellationToken ct = default);

        //This is for testing purposes only
        public Task<PagedProductResponse> GetPageResponseAsyncDeveloper(
      int? page, int? pageSize,string attributes_path,string products_path, CancellationToken ct = default);
        /// <summary>
        /// Get paginated lists from local JSON files
        /// </summary>
        /// <param name="attributeJsonPath"></param>
        /// <param name="productsJsonPath"></param>
        /// <param name="page"></param>
        /// <param name="pageSize"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<PagedResult<Product>> GetPageFromFilesAsync(
            string attributeJsonPath,
            string productsJsonPath,
            int? page,
            int? pageSize,
            CancellationToken cancellationToken = default);
    }
}
