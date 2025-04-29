using ProductsApi.Application.ErrorHandling;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ProductsApi.Shared
{

    /// <summary>
    /// Provides functionality to build lookup dictionaries for attributes and their values
    /// from a JSON structure. This class is designed to validate the input JSON and ensure
    /// that attribute and value codes are unique and conform to specific rules.
    /// </summary>
    /// <remarks>
    /// The JSON input is expected to be an array of attribute definitions, where each
    /// definition contains:
    /// - A "code" (string, lowercase letters only, unique per attribute)
    /// - A "name" (string, non-empty)
    /// - A "values" array, where each value contains:
    ///   - A "code" (string, unique per attribute, non-empty)
    ///   - A "name" (string, non-empty)
    ///
    /// </example>
    /// <exception cref="AttributeLookupException">
    /// Thrown when the input JSON is invalid or contains duplicate codes.
    /// </exception>
    public static class AttributeLookupBuilder
    {
        private static readonly Regex CodeRegex = new(@"^[a-z]+$", RegexOptions.Compiled);

        public static (
            Dictionary<string, string> AttrNameByCode,
            Dictionary<string, Dictionary<string, string>> ValueNameByCode
        ) BuildFromJson(JsonElement root, ILogger logger)
        {
            if (root.ValueKind != JsonValueKind.Array)
                throw new AttributeLookupException("Expected top‐level JSON array of attribute definitions");

            var attrNameByCode = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var valueNameByCode = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

            int defIndex = 0;
            foreach (var def in root.EnumerateArray())
            {
                defIndex++;

                // required props
                if (!def.TryGetProperty("code", out var codeProp)) throw new AttributeLookupException($"Definition #{defIndex} missing 'code'");
                if (!def.TryGetProperty("name", out var nameProp)) throw new AttributeLookupException($"Definition #{defIndex} missing 'name'");
                if (!def.TryGetProperty("values", out var valuesProp)) throw new AttributeLookupException($"Definition #{defIndex} missing 'values'");
                if (valuesProp.ValueKind != JsonValueKind.Array)
                    throw new AttributeLookupException($"Definition #{defIndex}: 'values' must be an array");

                // Validate attribute code
                if (codeProp.ValueKind != JsonValueKind.String)
                    throw new AttributeLookupException($"Definition #{defIndex}: 'code' must be a string");
                var attrCode = codeProp.GetString()!.Trim();
                if (string.IsNullOrEmpty(attrCode) || !CodeRegex.IsMatch(attrCode))
                    throw new AttributeLookupException($"Definition #{defIndex}: invalid 'code' '{attrCode}' (must be a–z only)");

                // Validate attribute name
                if (nameProp.ValueKind != JsonValueKind.String)
                    throw new AttributeLookupException($"Definition '{attrCode}': 'name' must be a string");
                var attrName = nameProp.GetString()!.Trim();
                if (string.IsNullOrEmpty(attrName))
                    throw new AttributeLookupException($"Definition '{attrCode}': 'name' cannot be empty");

                // **Duplicate attribute‐code check**
                if (!attrNameByCode.TryAdd(attrCode, attrName))
                {
                    logger.LogWarning("Duplicate attribute code '{AttrCode}' at definition #{DefIndex}", attrCode, defIndex);
                    throw new AttributeLookupException($"Duplicate attribute code '{attrCode}' found at definition #{defIndex}");
                }

                // Build the values map, checking for duplicate value codes
                var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                int valIndex = 0;
                foreach (var v in valuesProp.EnumerateArray())
                {
                    valIndex++;
                    if (!v.TryGetProperty("code", out var vcodeProp))
                        throw new AttributeLookupException($"Definition '{attrCode}', value #{valIndex} missing 'code'");
                    if (!v.TryGetProperty("name", out var vnameProp))
                        throw new AttributeLookupException($"Definition '{attrCode}', value #{valIndex} missing 'name'");

                    // Validate value code is a non-empty string
                    if (vcodeProp.ValueKind != JsonValueKind.String)
                        throw new AttributeLookupException(
                            $"Definition '{attrCode}', value #{valIndex}: 'code' must be a string");
                    var valueCode = vcodeProp.GetString()!.Trim();
                    if (string.IsNullOrEmpty(valueCode))
                        throw new AttributeLookupException(
                            $"Definition '{attrCode}', value #{valIndex}: 'code' cannot be empty");

                    // Validate value name
                    if (vnameProp.ValueKind != JsonValueKind.String)
                        throw new AttributeLookupException(
                            $"Definition '{attrCode}', value '{valueCode}': 'name' must be a string");
                    var valueName = vnameProp.GetString()!.Trim();
                    if (string.IsNullOrEmpty(valueName))
                        throw new AttributeLookupException(
                            $"Definition '{attrCode}', value '{valueCode}': 'name' cannot be empty");

                    // Duplicate value‐code check
                    if (!map.TryAdd(valueCode, valueName))
                    {
                        logger.LogWarning("Duplicate value code '{ValueCode}' for attribute '{AttrCode}' at definition #{DefIndex}, value #{ValIndex}",
                            valueCode, attrCode, defIndex, valIndex);
                        throw new AttributeLookupException(
                            $"Duplicate value code '{valueCode}' for attribute '{attrCode}' at definition #{defIndex}, value #{valIndex}");
                    }
                }

                valueNameByCode[attrCode] = map;
            }

            return (attrNameByCode, valueNameByCode);
        }
    }
}
