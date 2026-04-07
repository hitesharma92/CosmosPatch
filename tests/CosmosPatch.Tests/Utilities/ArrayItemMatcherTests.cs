using CosmosPatch.Application.Utilities;
using Newtonsoft.Json.Linq;

namespace CosmosPatch.Tests.Utilities;

public class ArrayItemMatcherTests
{
    private static JArray BuildArray()
    {
        return new JArray(
            JObject.FromObject(new { name = "Alice", role = "admin" }),
            JObject.FromObject(new { name = "Bob", role = "user" }),
            JObject.FromObject(new { name = "Charlie", role = "user" })
        );
    }

    [Fact]
    public void GetIndexOfMatchingObject_ReturnsCorrectIndex()
    {
        JArray array = BuildArray();
        Dictionary<string, object> criteria = new() { { "name", "Bob" } };

        int index = ArrayItemMatcher.GetIndexOfMatchingObject(array, criteria);

        Assert.Equal(1, index);
    }

    [Fact]
    public void GetIndexOfMatchingObject_ReturnsNegativeOne_WhenNotFound()
    {
        JArray array = BuildArray();
        Dictionary<string, object> criteria = new() { { "name", "Diana" } };

        int index = ArrayItemMatcher.GetIndexOfMatchingObject(array, criteria);

        Assert.Equal(-1, index);
    }

    [Fact]
    public void GetIndexOfMatchingObject_MultipleProperties_MatchesCorrectEntry()
    {
        JArray array = BuildArray();
        Dictionary<string, object> criteria = new() { { "name", "Bob" }, { "role", "user" } };

        int index = ArrayItemMatcher.GetIndexOfMatchingObject(array, criteria);

        Assert.Equal(1, index);
    }

    [Fact]
    public void GetIndexOfMatchingObject_MultipleProperties_NoMatchWhenValueDiffers()
    {
        JArray array = BuildArray();
        Dictionary<string, object> criteria = new() { { "name", "Alice" }, { "role", "user" } };

        int index = ArrayItemMatcher.GetIndexOfMatchingObject(array, criteria);

        Assert.Equal(-1, index);
    }

    [Fact]
    public void GetMatchingObjects_ReturnsAll_WithSameRole()
    {
        JArray array = BuildArray();
        Dictionary<string, object> criteria = new() { { "role", "user" } };

        List<JObject> matches = ArrayItemMatcher.GetMatchingObjects(array, criteria);

        Assert.Equal(2, matches.Count);
        Assert.Contains(matches, x => x["name"]!.ToString() == "Bob");
        Assert.Contains(matches, x => x["name"]!.ToString() == "Charlie");
    }

    [Fact]
    public void GetMatchingObjects_ReturnsEmpty_WhenNoneMatch()
    {
        JArray array = BuildArray();
        Dictionary<string, object> criteria = new() { { "role", "superadmin" } };

        List<JObject> matches = ArrayItemMatcher.GetMatchingObjects(array, criteria);

        Assert.Empty(matches);
    }
}
