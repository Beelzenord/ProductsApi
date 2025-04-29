using ProductsApi.Application.ErrorHandling;
using ProductsApi.Application.Strategy;
using ProductsApi.Domain.Entities.Data;
using System.Reflection.Metadata.Ecma335;

namespace ProductsApi.Domain.Strategy
{
    public class FlatAttributeMappingStrategy : IAttributeMappingStrategy
    {
        private IReadOnlyDictionary<string, string> _attrNames = null!;
        private IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> _valueNames = null!;

        public void Configure(
            IReadOnlyDictionary<string, string> attrNames,
            IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> valueNames)
        {
            _attrNames = attrNames ?? throw new ArgumentNullException(nameof(attrNames));
            _valueNames = valueNames ?? throw new ArgumentNullException(nameof(valueNames));
        }

        public bool CanMap(string attributeCode) => attributeCode != "cat";

        public IEnumerable<ResolvedAttr> Map(string attributeCode, string rawValues)
        {
            // 1) Ensure Configure was called
            if (_attrNames == null || _valueNames == null)
                throw new AttributeMappingException(
                    "FlatAttributeMappingStrategy not configured with lookup dictionaries");

            // 2) Look up the display name for this attribute
            if (!_attrNames.TryGetValue(attributeCode, out var displayName))
                throw new AttributeMappingException(
                 $"Attribute code '{attributeCode}' not found in lookup");

            // 3) Split and map each individual code
            var codes = rawValues?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        ?? Array.Empty<string>();

            foreach (var code in codes)
            {
                // 4) Look up the friendly value name
                if (!_valueNames.TryGetValue(attributeCode, out var valueMap))
                    throw new KeyNotFoundException($"No value‐map found for attribute '{attributeCode}'");

                if (!valueMap.TryGetValue(code, out var valueName))
                    throw new KeyNotFoundException(
                        $"Value code '{code}' not found for attribute '{attributeCode}'");

                ResolvedAttr resolvedAttr;
                try
                {
                    resolvedAttr = new ResolvedAttr
                    {
                        AttributeCode = attributeCode,
                        AttributeName = displayName,
                        SelectedValues = new List<string> { valueName }
                    };
                }
                catch (KeyNotFoundException ex)
                {
                    // wrap missing-code errors
                    throw new StrategyImplementationException(
                        $"Flat mapping failed for attribute '{attributeCode}': {ex.Message}", ex);
                }
                catch (Exception ex)
                {
                    // catch any other unexpected errors
                    throw new StrategyImplementationException(
                        $"Unexpected error mapping attribute '{attributeCode}'", ex);
                }

                yield return resolvedAttr;
            }
        }
    }
}
