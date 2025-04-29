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

        public Task<PagedResult<Product>> GetPagesAsync(Stream productsStream, int? page, int? pageSize, CancellationToken ct = default);

    }
}
