using ProductsApi.Application.ErrorHandling;
using ProductsApi.Application.Resolvers;
using ProductsApi.Domain.Entities.Data;
using ProductsApi.Domain.Entities.Mappings;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace ProductsApi.Services
{
    public class ProductResolver : IProductResolver
    {
        private IReadOnlyDictionary<string, string> _attrNames = null!;
        private IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> _valueNames = null!;
        private ICategoryPathBuilder _catBuilder = null!;

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

        public async Task<PagedResult<Product>> GetPagesAsyncAlt(Stream productsStream, int? page, int? pageSize, CancellationToken ct = default)
        {
            // determine if we’re paginating or returning everything
            var isPaging = page.HasValue && pageSize.HasValue;
            if (isPaging && (page < 1 || pageSize < 1))
                throw new ArgumentException("Page and pageSize must be >= 1 when supplied.");

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var pageItems = new List<Product>(pageSize ?? 0);
            int totalCount = 0;

            // compute window only if paging
            int startIndex = isPaging
                ? ((page!.Value - 1) * pageSize!.Value + 1) // Use the null-forgiving operator (!) to suppress nullable warnings
                : int.MinValue;
            int endIndex = isPaging
                ? (startIndex + pageSize!.Value - 1) // Use the null-forgiving operator (!) to suppress nullable warnings
                : int.MaxValue;

            await foreach (var raw in JsonSerializer
                .DeserializeAsyncEnumerable<ProductRaw>(productsStream, options, ct)
                .Where(r => r is not null))
            {
                totalCount++;

                // if paged, skip until we hit our window
                if (isPaging
                    && (totalCount < startIndex || totalCount > endIndex))
                {
                    continue;
                }

                // map & resolve attributes as before
                var resolvedAttrs = raw!.Attributes.Select(kv =>
                {
                    var code = kv.Key;
                    var codes = kv.Value.Split(',', StringSplitOptions.RemoveEmptyEntries);

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

            // assemble the result
            return new PagedResult<Product>
            {
                Items = pageItems,
                TotalCount = totalCount,

                // if paging, use their values; otherwise return the full list as page 1
                Page = isPaging ? page!.Value : 1,
                PageSize = isPaging ? pageSize!.Value : totalCount
            };
        }
    }
}
