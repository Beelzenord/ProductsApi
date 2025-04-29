using ProductsApi.Application.ErrorHandling;
using ProductsApi.Application.Resolvers;
using ProductsApi.Application.Strategy;
using ProductsApi.Domain.Entities.Data;
using ProductsApi.Domain.Entities.Mappings;

namespace ProductsApi.Domain.Strategy
{
    // 2) Hardened strategy implementation
    public class CategoryMappingStrategy : IAttributeMappingStrategy
    {

        private IReadOnlyDictionary<string, string>? _attrNames;
        private IReadOnlyDictionary<string, string>? _catMap;
        private CategoryPathBuilder? _builder;

        public void Configure(
            IReadOnlyDictionary<string, string> attrNames,
            IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> valueNames)
        {
            if (attrNames == null) throw new ArgumentNullException(nameof(attrNames));
            if (valueNames == null) throw new ArgumentNullException(nameof(valueNames));

            // 1) Grab the raw "cat" map or fail immediately
            if (!valueNames.TryGetValue("cat", out var catMap))
                throw new StrategyImplementationException("Missing 'cat' entry in attribute_meta");

            _attrNames = attrNames;
            _catMap = catMap;
            _builder = new CategoryPathBuilder(catMap);
        }

        public bool CanMap(string attributeCode) => attributeCode == "cat";

        public IEnumerable<ResolvedAttr> Map(string attributeCode, string rawValues)
        {
            // 1) Ensure we've been configured
            if (_attrNames == null || _builder == null || _catMap == null)
                throw new StrategyImplementationException(
                    "CategoryMappingStrategy not configured; call Configure() first.");

            // 2) Look up attribute display name
            if (!_attrNames.TryGetValue(attributeCode, out var displayName))
                throw new KeyNotFoundException(
                    $"Attribute code '{attributeCode}' not found in attrNames");

            // 3) Split raw values
            var codes = rawValues?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        ?? Array.Empty<string>();

            // 4) Build one ResolvedAttr per code
            foreach (var code in codes)
            {
                if (!_catMap.ContainsKey(code))
                    throw new StrategyImplementationException(
                        $"Unknown category code '{code}' for attribute 'cat'.");

                string path;
                try
                {
                    path = _builder.Build(code);
                }
                catch (Exception ex)
                {
                    throw new StrategyImplementationException(
                        $"Failed building category path for code '{code}'", ex);
                }

                yield return new ResolvedAttr
                {
                    AttributeCode = attributeCode,
                    AttributeName = displayName,
                    SelectedValues = new List<string> { path }
                };
            }
        }
    }
}
