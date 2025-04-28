using ProductsApi.Application.Resolvers;
using ProductsApi.Application.Strategy;
using ProductsApi.Domain.Entities.Data;
using ProductsApi.Domain.Entities.Mappings;

namespace ProductsApi.Domain.Strategy
{
    public class CategoryMappingStrategy : IAttributeMappingStrategy
    {
        private IReadOnlyDictionary<string, string> _attrNames = null!;
        private CategoryPathBuilder _builder = null!;

        public void Configure(
            IReadOnlyDictionary<string, string> attrNames,
            IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> valueNames)
        {
            _attrNames = attrNames;
            _builder = new CategoryPathBuilder(valueNames["cat"]);
        }

        public bool CanMap(string attributeCode) => attributeCode == "cat";

        public IEnumerable<ResolvedAttr> Map(string attributeCode, string rawValues)
        {
            var display = _attrNames[attributeCode];
            return rawValues
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(code => new ResolvedAttr
                {
                    AttributeCode = attributeCode,
                    AttributeName = display,
                    SelectedValues = new List<string> { _builder.Build(code) }
                });
        }
    }
}
