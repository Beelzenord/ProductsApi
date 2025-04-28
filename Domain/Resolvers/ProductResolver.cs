using ProductsApi.Application.ErrorHandling;
using ProductsApi.Application.Resolvers;
using ProductsApi.Application.Strategy;
using ProductsApi.Domain.Entities.Data;
using ProductsApi.Domain.Entities.Mappings;
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
       int page,
       int pageSize,
       CancellationToken ct = default)
        {
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var pageItems = new List<Product>(pageSize);
                int totalCount = 0;
                int startIndex = (page - 1) * pageSize + 1;
                int endIndex = startIndex + pageSize - 1;

                await foreach (var raw in JsonSerializer
                    .DeserializeAsyncEnumerable<ProductRaw>(productsStream, options, ct)
                    .Where(r => r is not null))
                {
                    totalCount++;
                    if (totalCount < startIndex || totalCount > endIndex)
                        continue;

                    // mapping can throw, so it's inside our try/catch
                    var resolvedAttrs = raw!.Attributes.Select(kv =>
                    {
                        var code = kv.Key;
                        var codes = kv.Value
                                      .Split(',', StringSplitOptions.RemoveEmptyEntries);

                        if (code == "cat")
                        {
                            return new ResolvedAttr
                            {
                                AttributeCode = code,
                                AttributeName = _attrNames[code],
                                SelectedValues = codes.Select(_catBuilder.Build).ToList()
                            };
                        }

                        return new ResolvedAttr
                        {
                            AttributeCode = code,
                            AttributeName = _attrNames[code],
                            SelectedValues = codes.Select(c => _valueNames[code][c]).ToList()
                        };
                    }).ToList();

                    pageItems.Add(new Product
                    {
                        Id = raw.Id,
                        Name = raw.Name,
                        Attributes = resolvedAttrs
                    });
                }

                return new PagedResult<Product>
                {
                    Items = pageItems,
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = totalCount
                };
            }
            catch (JsonException ex)
            {
                throw new ProductResolutionException(
                    "Failed to parse products JSON", ex);
            }
            catch (KeyNotFoundException ex)
            {
                throw new ProductResolutionException(
                    "Unexpected attribute code encountered during product mapping", ex);
            }
            catch (OperationCanceledException)
            {
                // let cancellations bubble up unchanged
                throw;
            }
            catch (Exception ex)
            {
                throw new ProductResolutionException(
                    "An unexpected error occurred while resolving products", ex);
            }
        }

        public async Task<PagedResult<Product>> GetPagesAsyncAlt(
         Stream productsStream,
         int? page,
         int? pageSize,
         CancellationToken ct = default)
        {
            var isPaging = page.HasValue && pageSize.HasValue;
            if (isPaging && (page! < 1 || pageSize! < 1))
                throw new ArgumentException("page and pageSize must be >= 1");

            int skip = isPaging
                ? (page!.Value - 1) * pageSize!.Value
                : 0;
            int take = pageSize ?? int.MaxValue;

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var items = new List<Product>(pageSize ?? 0);
            int total = 0;

            await foreach (var raw in JsonSerializer
                .DeserializeAsyncEnumerable<ProductRaw>(productsStream, options, ct)
                .Where(r => r is not null))
            {
                total++;
                if (total <= skip)
                    continue;

                // ** Strategy‐based mapping ** 
                var resolvedAttrs = raw!.Attributes
                    .SelectMany(kv =>
                    {
                        // pick the first strategy that knows this code
                        var strat = _strategies.First(s => s.CanMap(kv.Key));
                        return strat.Map(kv.Key, kv.Value);
                    })
                    .ToList();

                items.Add(new Product
                {
                    Id = raw.Id,
                    Name = raw.Name,
                    Attributes = resolvedAttrs
                });

                if (isPaging && items.Count >= take)
                    break;
            }

            return new PagedResult<Product>
            {
                Items = items,
                TotalCount = total,
                Page = isPaging ? page!.Value : 1,
                PageSize = isPaging ? take : total
            };
        }
    }
}
