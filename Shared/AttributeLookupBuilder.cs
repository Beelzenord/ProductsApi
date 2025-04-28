using ProductsApi.Application.ErrorHandling;
using System.Text.Json;

namespace ProductsApi.Shared
{

    public static class AttributeLookupBuilder
    {
        /// <summary>
        /// Parses the attribute‐definitions JSON and builds:
        ///  1) a map from attribute code → attribute display name
        ///  2) a map from attribute code → (value code → value display name)
        /// </summary>
        public static (
            Dictionary<string, string> AttrNameByCode,
            Dictionary<string, Dictionary<string, string>> ValueNameByCode
        ) BuildFromJson(JsonElement root)
        {
            try
            {
                if (root.ValueKind != JsonValueKind.Array)
                    throw new AttributeLookupException("Expected JSON array of attribute definitions");

                var attrNameByCode = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var valueNameByCode = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

                int defIndex = 0;
                foreach (var def in root.EnumerateArray())
                {
                    defIndex++;
                    try
                    {
                        // Required properties
                        if (!def.TryGetProperty("code", out var codeProp) ||
                            !def.TryGetProperty("name", out var nameProp) ||
                            !def.TryGetProperty("values", out var valuesProp) ||
                             valuesProp.ValueKind != JsonValueKind.Array)
                        {
                            // skip malformed entries
                            continue;
                        }

                        var attrCode = codeProp.GetString()
                                       ?? throw new AttributeLookupException($"Attribute 'code' was null at definition #{defIndex}");
                        var attrName = nameProp.GetString()
                                       ?? throw new AttributeLookupException($"Attribute 'name' was null for code '{attrCode}'");

                        attrNameByCode[attrCode] = attrName;

                        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        int valIndex = 0;
                        foreach (var v in valuesProp.EnumerateArray())
                        {
                            valIndex++;
                            try
                            {
                                if (!v.TryGetProperty("code", out var vcodeProp) ||
                                    !v.TryGetProperty("name", out var vnameProp))
                                {
                                    continue; // skip bad value entries
                                }

                                var valueCode = vcodeProp.GetString()
                                                ?? throw new AttributeLookupException(
                                                    $"Value 'code' was null for attribute '{attrCode}', value #{valIndex}");
                                var valueName = vnameProp.GetString()
                                                ?? throw new AttributeLookupException(
                                                    $"Value 'name' was null for attribute '{attrCode}', value '{valueCode}'");

                                map[valueCode] = valueName;
                            }
                            catch (AttributeLookupException)
                            {
                                // rethrow our custom exceptions directly
                                throw;
                            }
                            catch (Exception ex)
                            {
                                throw new AttributeLookupException(
                                    $"Error parsing value #{valIndex} for attribute '{attrCode}'", ex);
                            }
                        }

                        valueNameByCode[attrCode] = map;
                    }
                    catch (AttributeLookupException)
                    {
                        // bubble up our own exceptions
                        throw;
                    }
                    catch (Exception ex)
                    {
                        throw new AttributeLookupException(
                            $"Error parsing attribute definition at index #{defIndex}", ex);
                    }
                }

                return (attrNameByCode, valueNameByCode);
            }
            catch (AttributeLookupException)
            {
                // bubble our exceptions unchanged
                throw;
            }
            catch (Exception ex)
            {
                // catch anything else (e.g. JsonElement quirks)
                throw new AttributeLookupException(
                    "Failed to build attribute lookups from JSON", ex);
            }
        }
    }
}
