using ProductsApi.Domain.Entities.Data;

namespace ProductsApi.Application.Strategy
{
    /// <summary>
    /// Defines the contract for mapping raw attribute data to resolved attributes
    /// using pre-configured lookup dictionaries.
    /// </summary>
    public interface IAttributeMappingStrategy
    {
        /// <summary>
        /// Configures the strategy with lookup dictionaries for attribute names and value names.
        /// </summary>
        /// <param name="attrNames">
        /// A dictionary mapping attribute codes to their corresponding attribute names.
        /// </param>
        /// <param name="valueNames">
        /// A dictionary mapping attribute codes to dictionaries of value codes and their corresponding value names.
        /// </param>
        void Configure(
           IReadOnlyDictionary<string, string> attrNames,
           IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> valueNames
       );
        /// <summary>
        /// Determines whether the strategy can map a given attribute code.
        /// </summary>
        /// <param name="attributeCode">The code of the attribute to check.</param>
        /// <returns>
        /// <c>true</c> if the strategy can map the specified attribute code; otherwise, <c>false</c>.
        /// </returns>
        bool CanMap(string attributeCode);
        /// <summary>
        /// Maps raw attribute values to resolved attributes using the configured lookup dictionaries.
        /// </summary>
        /// <param name="attributeCode">The code of the attribute to map.</param>
        /// <param name="rawValues">The raw values to map for the specified attribute.</param>
        /// <returns>
        /// An <see cref="IEnumerable{ResolvedAttr}"/> containing the resolved attributes and their selected values.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the strategy has not been configured or if the attribute code cannot be mapped.
        /// </exception>
        IEnumerable<ResolvedAttr> Map(string attributeCode, string rawValues);
    }
}
