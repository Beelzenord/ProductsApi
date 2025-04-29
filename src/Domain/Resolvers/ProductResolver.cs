using ProductsApi.Application.ErrorHandling;
using ProductsApi.Application.Resolvers;
using ProductsApi.Application.Strategy;
using ProductsApi.Domain.Entities.Data;
using ProductsApi.Domain.Entities.Mappings;

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
        {
            _strategies = strategies;
        }

        public void ConfigureLookups(
            IReadOnlyDictionary<string, string> attrNames,
            IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> valueNames)
        {
            try
            {
                _attrNames = attrNames ?? throw new ArgumentNullException(nameof(attrNames));
                _valueNames = valueNames ?? throw new ArgumentNullException(nameof(valueNames));

                // Ensure we have a "cat" definition (category)
                if (!_valueNames.TryGetValue("cat", out var catMap))
                    throw new KeyNotFoundException("Attribute code 'cat' not found in definitions");

                _catBuilder = new CategoryPathBuilder(catMap);

                foreach (var strat in _strategies)
                {
                    strat.Configure(attrNames, valueNames);
                }
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
            var isPaging = page.HasValue && pageSize.HasValue;
            if (isPaging && (page! < 1 || pageSize! < 1))
                throw new ArgumentException("page and pageSize must be >= 1");

            int skip = isPaging
                ? (page!.Value - 1) * pageSize!.Value
                : 0;
            int take = pageSize ?? int.MaxValue;

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var docOpts = new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip };

            var items = new List<Product>(pageSize ?? 0);
            var warnings = new List<string>();
            int total = 0;

            using var doc = await JsonDocument.ParseAsync(productsStream, docOpts, ct);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                throw new ProductResolutionException("Expected top‐level JSON array of products");

            foreach (var elem in doc.RootElement.EnumerateArray())
            {
                // 1) Always count every element
                total++;

                // 2) Skip elements before your page window
                if (total <= skip)
                    continue;

                // 3) After page is full, skip mapping but keep counting
                if (isPaging && items.Count >= take)
                    continue;

                // 4) Your existing validation + mapping…
                if (!elem.TryGetProperty("id", out var idProp) || idProp.ValueKind != JsonValueKind.Number)
                {
                    warnings.Add($"Product[{total}]: missing or invalid 'id'");
                    continue;
                }
                var id = idProp.GetInt32();

                if (!elem.TryGetProperty("name", out var nameProp)
                    || nameProp.ValueKind != JsonValueKind.String
                    || string.IsNullOrWhiteSpace(nameProp.GetString()))
                {
                    warnings.Add($"Product {id}: missing or empty 'name'");
                    continue;
                }
                var name = nameProp.GetString()!.Trim();

                if (!elem.TryGetProperty("attributes", out var attrProp)
                    || attrProp.ValueKind != JsonValueKind.Object)
                {
                    warnings.Add($"Product {id}: missing 'attributes' object");
                    continue;
                }

                var productWarnings = new List<string>();
                var resolved = new List<ResolvedAttr>();

                foreach (var p in attrProp.EnumerateObject())
                {
                    var code = p.Name;
                    var raw = p.Value.GetString() ?? "";

                    try
                    {
                        var strat = _strategies.FirstOrDefault(s => s.CanMap(code))
                                    ?? throw new Exception($"No mapping strategy for '{code}'");
                        resolved.AddRange(strat.Map(code, raw));
                    }
                    catch (Exception ex)
                    {
                        productWarnings.Add($"Attribute '{code}': {ex.Message}");
                    }
                }

                // Skip products with any mapping warnings
                if (productWarnings.Count > 0)
                {
                    warnings.Add($"Product {id}: {string.Join("; ", productWarnings)}");
                    continue;
                }

                // Add to the page
                items.Add(new Product
                {
                    Id = id,
                    Name = name,
                    Attributes = resolved
                });
            }

            return new PagedResult<Product>
            {
                Items = items,
                Warnings = warnings.Count > 0 ? warnings : null,
                TotalCount = total,                       // now the true source total
                Page = isPaging ? page!.Value : 1,
                PageSize = isPaging ? take : total
            };
        }
    }
}