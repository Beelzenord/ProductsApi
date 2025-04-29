using ProductsApi.Application.ErrorHandling;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ProductsApi.Shared
{


    public static class AttributeLookupBuilder
    {
        // only lowercase a–z, one or more
        private static readonly Regex CodeRegex = new(@"^[a-z]+$", RegexOptions.Compiled);

        public static (
            Dictionary<string, string> AttrNameByCode,
            Dictionary<string, Dictionary<string, string>> ValueNameByCode
        ) BuildFromJson(JsonElement root)
        {
            if (root.ValueKind != JsonValueKind.Array)
                throw new AttributeLookupException("Expected top-level JSON array of attribute definitions");

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
                    throw new AttributeLookupException($"Definition #{defIndex}: 'values' must be an array (can be empty)");

                // validate attribute code
                if (codeProp.ValueKind != JsonValueKind.String)
                    throw new AttributeLookupException($"Definition #{defIndex}: 'code' must be a string");
                var attrCode = codeProp.GetString()!.Trim();
                if (string.IsNullOrEmpty(attrCode) || !CodeRegex.IsMatch(attrCode))
                    throw new AttributeLookupException($"Definition #{defIndex}: invalid 'code' value '{attrCode}' (must be a–z only)");

                // validate attribute name
                if (nameProp.ValueKind != JsonValueKind.String)
                    throw new AttributeLookupException($"Definition '{attrCode}': 'name' must be a string");
                var attrName = nameProp.GetString()!.Trim();
                if (string.IsNullOrEmpty(attrName))
                    throw new AttributeLookupException($"Definition '{attrCode}': 'name' cannot be empty");

                attrNameByCode[attrCode] = attrName;

                // build each value mapping (values may now be any non-empty string)
                var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                int valIndex = 0;
                foreach (var v in valuesProp.EnumerateArray())
                {
                    valIndex++;
                    if (!v.TryGetProperty("code", out var vcodeProp))
                        throw new AttributeLookupException($"Definition '{attrCode}', value #{valIndex} missing 'code'");
                    if (!v.TryGetProperty("name", out var vnameProp))
                        throw new AttributeLookupException($"Definition '{attrCode}', value #{valIndex} missing 'name'");

                    // validate value code is a non-empty string (no regex)
                    if (vcodeProp.ValueKind != JsonValueKind.String)
                        throw new AttributeLookupException(
                            $"Definition '{attrCode}', value #{valIndex}: 'code' must be a string");
                    var valueCode = vcodeProp.GetString()!.Trim();
                    if (string.IsNullOrEmpty(valueCode))
                        throw new AttributeLookupException(
                            $"Definition '{attrCode}', value #{valIndex}: 'code' cannot be empty");

                    // validate value name
                    if (vnameProp.ValueKind != JsonValueKind.String)
                        throw new AttributeLookupException(
                            $"Definition '{attrCode}', value '{valueCode}': 'name' must be a string");
                    var valueName = vnameProp.GetString()!.Trim();
                    if (string.IsNullOrEmpty(valueName))
                        throw new AttributeLookupException(
                            $"Definition '{attrCode}', value '{valueCode}': 'name' cannot be empty");

                    map[valueCode] = valueName;
                }

                valueNameByCode[attrCode] = map;
            }

            return (attrNameByCode, valueNameByCode);
        }
    }
}
