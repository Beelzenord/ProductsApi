using ProductsApi.Domain.Entities.Data;
using ProductsApi.Domain.Entities.Mappings;

namespace ProductsApi.Application.Resolvers
{
    public interface IProductResolver
    {
        /// <summary>Supply lookup dictionaries before calling Resolve…</summary>
        void ConfigureLookups(
            IReadOnlyDictionary<string, string> attrNames,
            IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> valueNames);
        /// <summary>
        /// Converts the stream of JSON data into a paginated list of products.
        /// </summary>
        /// <param name="productsStream">The stream containing the product data in JSON or another supported format.</param>
        /// <param name="page">The page number to retrieve (optional).</param>
        /// <param name="pageSize">The number of items per page (optional).</param>
        /// <param name="ct">A cancellation token to cancel the operation (optional).</param>
        /// <returns>
        /// A <see cref="PagedResult{Product}"/> containing the paginated list of products,
        /// along with metadata such as the total number of pages and any warnings.
        /// </returns>
        /// <remarks>
        /// This method processes the product data from the provided stream, applies pagination,
        /// and returns the results. Ensure that the stream is properly formatted and that
        /// <see cref="ConfigureLookups"/> has been called to supply the necessary lookup dictionaries
        /// before invoking this method.
        /// </remarks>
        public Task<PagedResult<Product>> GetPagesAsync(Stream productsStream, int? page, int? pageSize, CancellationToken ct = default);

    }
}
