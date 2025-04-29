using ProductsApi.Application.ErrorHandling;
using ProductsApi.Application.Resolvers;

namespace ProductsApi.Domain.Entities.Mappings
{
    public class CategoryPathBuilder : ICategoryPathBuilder
    {
        private readonly IReadOnlyDictionary<string, string> _valueMap;

        public CategoryPathBuilder(IReadOnlyDictionary<string, string> categoryValueMap)
        {
            _valueMap = categoryValueMap
                ?? throw new ArgumentNullException(nameof(categoryValueMap));
        }

        public string Build(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("Category code must be non-empty", nameof(code));

            try
            {
                var parts = new List<string>();
                var curr = code;

                while (_valueMap.TryGetValue(curr, out var name))
                {
                    parts.Insert(0, name);
                    var idx = curr.LastIndexOf('_');
                    if (idx <= 0) break;
                    curr = curr.Substring(0, idx);
                }

                return string.Join(" > ", parts);
            }
            catch (KeyNotFoundException ex)
            {
                throw new CategoryPathBuilderException(
                    $"Unknown category code '{code}' encountered", ex);
            }
            catch (Exception ex)
            {
                throw new CategoryPathBuilderException(
                    $"Error building category path for code '{code}'", ex);
            }
        }
    }
}