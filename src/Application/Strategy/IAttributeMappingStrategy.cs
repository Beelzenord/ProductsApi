using ProductsApi.Domain.Entities.Data;

namespace ProductsApi.Application.Strategy
{
    public interface IAttributeMappingStrategy
    {
        void Configure(
           IReadOnlyDictionary<string, string> attrNames,
           IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> valueNames
       );

        bool CanMap(string attributeCode);
        IEnumerable<ResolvedAttr> Map(string attributeCode, string rawValues);
    }
}
