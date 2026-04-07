using Newtonsoft.Json.Linq;

namespace CosmosPatch.Application.Utilities;

/// <summary>
/// Provides shared utilities for finding matching objects inside a JArray.
/// Extracted from the original duplicated code in PatchChildArrayItem and PatchRemoveChildArrayItem.
/// </summary>
public static class ArrayItemMatcher
{
    /// <summary>
    /// Returns the 0-based index of the first JObject in <paramref name="array"/>
    /// whose properties all match the given key-value pairs.
    /// Returns -1 if not found.
    /// </summary>
    public static int GetIndexOfMatchingObject(JArray array, Dictionary<string, object> properties)
    {
        List<JObject> matches = GetMatchingObjects(array, properties);
        JObject? first = matches.FirstOrDefault();
        if (first is null) return -1;

        JTokenEqualityComparer comparer = new();
        return Array.FindIndex(array.ToArray(), x => comparer.Equals(x, first));
    }

    /// <summary>
    /// Returns all JObjects in <paramref name="array"/> whose properties match
    /// all the given key-value pairs (string comparison).
    /// </summary>
    public static List<JObject> GetMatchingObjects(JArray array, Dictionary<string, object> properties)
    {
        List<JObject> items = array.ToObject<List<JObject>>() ?? new List<JObject>();
        List<JObject> matches = new();

        foreach (JObject obj in items)
        {
            bool allMatch = true;
            foreach (KeyValuePair<string, object> kv in properties)
            {
                JToken? token = obj.GetValue(kv.Key);
                if (token is null || token.ToString() != kv.Value?.ToString())
                {
                    allMatch = false;
                    break;
                }
            }
            if (allMatch) matches.Add(obj);
        }

        return matches;
    }
}
