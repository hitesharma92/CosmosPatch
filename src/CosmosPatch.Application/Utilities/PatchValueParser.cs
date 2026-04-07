using Newtonsoft.Json.Linq;

namespace CosmosPatch.Application.Utilities;

/// <summary>
/// Parses Excel cell values into the appropriate types for Cosmos patch operations.
/// </summary>
public static class PatchValueParser
{
    /// <summary>
    /// Interprets a DataRow cell value for inclusion in a patch operation.
    /// - DBNull → null
    /// - "[]" string → empty JArray
    /// - "{...}" string → JObject.Parse(...)
    /// - everything else → the raw value
    /// </summary>
    public static object? Parse(object cellValue)
    {
        if (cellValue == DBNull.Value || cellValue is null)
            return null;

        if (cellValue is string strValue)
        {
            if (strValue == "[]")
                return new JArray();

            if (strValue.StartsWith("{") && strValue.EndsWith("}"))
                return JObject.Parse(strValue);
        }

        return cellValue;
    }
}
