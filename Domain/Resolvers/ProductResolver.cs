using ProductsApi.Application.ErrorHandling;
using ProductsApi.Application.Resolvers;
using ProductsApi.Application.Strategy;
using ProductsApi.Domain.Entities.Data;
using ProductsApi.Domain.Entities.Mappings;
using ProductsApi.Domain.Strategy;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace ProductsApi.Domain.Resolvers
{
    public class ProductResolver : IProductResolver
    {
        private IReadOnlyDictionary<string, string> _attrNames = null!;
        private IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> _valueNames = null!;
        private ICategoryPathBuilder _catBuilder = null!;
        private readonly IEnumerable<IAttributeMappingStrategy> _strategies;
        public ProductResolver(IEnumerable<IAttributeMappingStrategy> strategies)
            => _strategies = strategies;
        public void ConfigureLookups(
            IReadOnlyDictionary<string, string> attrNames,
            IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> valueNames)
        {
            try
            {
                _attrNames = attrNames ?? throw new ArgumentNullException(nameof(attrNames));
                _valueNames = valueNames ?? throw new ArgumentNullException(nameof(valueNames));

                // ensure we have a "cat" definition
                if (!_valueNames.TryGetValue("cat", out var catMap))
                    throw new KeyNotFoundException("Attribute code 'cat' not found in definitions");

                _catBuilder = new CategoryPathBuilder(catMap);

                foreach (var strat in _strategies)
                    strat.Configure(attrNames, valueNames);
            }
            catch (Exception ex) when (
                ex is ArgumentNullException ||
                ex is KeyNotFoundException)
            {
                throw new ResolverConfigurationException(
                    "Failed to configure ProductResolver lookups", ex);
            }
        }

        public async Task<PagedResult<Product>> GetPagesAsync(
     Stream productsStream,
     int? page,
     int? pageSize,
     CancellationToken ct = default)
        {
            // 1) Prepare paging parameters
            var isPaging = page.HasValue && pageSize.HasValue;
            if (isPaging && (page! < 1 || pageSize! < 1))
                throw new ArgumentException("page and pageSize must be >= 1");

            int skip = isPaging
                ? (page!.Value - 1) * pageSize!.Value
                : 0;
            int take = pageSize ?? int.MaxValue;

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // 2) Local accumulators
            var items = new List<Product>(pageSize ?? 0);
            var warnings = new List<string>();
            int total = 0;

            // 3) One‐pass streaming
            await foreach (var raw in JsonSerializer
                .DeserializeAsyncEnumerable<ProductRaw>(productsStream, options, ct)
                .Where(r => r is not null))
            {
                total++;
                if (total <= skip)
                    continue;

                // 3a) Per‐product warning bucket
                var productWarnings = new List<string>();

                // 3b) Map each attribute, catching _any_ failure
                var resolved = new List<ResolvedAttr>();
                foreach (var kv in raw!.Attributes)
                {
                    try
                    {
                        var strat = _strategies.FirstOrDefault(s => s.CanMap(kv.Key))
                                    ?? throw new Exception($"No strategy for '{kv.Key}'");
                        resolved.AddRange(strat.Map(kv.Key, kv.Value));
                    }
                    catch (Exception ex)
                    {
                        // record and skip
                        productWarnings.Add(ex.Message);
                    }
                }

                // 3c) Add the product (even if 'resolved' is empty)
                items.Add(new Product
                {
                    Id = raw.Id,
                    Name = raw.Name,
                    Attributes = resolved
                });

                // 3d) Record product‐specific warnings
                if (productWarnings.Count > 0)
                {
                    warnings.Add(
                        $"Product {raw.Id}: {string.Join("; ", productWarnings)}"
                    );
                }

                // 3e) Early exit for paging
                if (isPaging && items.Count >= take)
                    break;
            }

            // 4) Build and return the result in one shot
            return new PagedResult<Product>
            {
                Items = items,
                Warnings = warnings,
                TotalCount = total,
                Page = isPaging ? page!.Value : 1,
                PageSize = isPaging ? take : total
            };
        }
    }
}