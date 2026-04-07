using CosmosPatch.Application.Utilities;
using Newtonsoft.Json.Linq;

namespace CosmosPatch.Tests.Utilities;

public class PatchValueParserTests
{
    [Fact]
    public void Parse_DBNull_ReturnsNull()
    {
        object? result = PatchValueParser.Parse(DBNull.Value);
        Assert.Null(result);
    }

    [Fact]
    public void Parse_Null_ReturnsNull()
    {
        object? result = PatchValueParser.Parse(null!);
        Assert.Null(result);
    }

    [Fact]
    public void Parse_EmptyArrayString_ReturnsJArray()
    {
        object? result = PatchValueParser.Parse("[]");
        Assert.IsType<JArray>(result);
        Assert.Empty((JArray)result!);
    }

    [Fact]
    public void Parse_JsonObjectString_ReturnsJObject()
    {
        object? result = PatchValueParser.Parse("{\"key\":\"value\"}");
        Assert.IsType<JObject>(result);
        JObject obj = (JObject)result!;
        Assert.Equal("value", obj["key"]!.ToString());
    }

    [Fact]
    public void Parse_PlainString_ReturnsSameString()
    {
        object? result = PatchValueParser.Parse("hello world");
        Assert.Equal("hello world", result);
    }

    [Fact]
    public void Parse_Integer_ReturnsInteger()
    {
        object? result = PatchValueParser.Parse(42);
        Assert.Equal(42, result);
    }

    [Fact]
    public void Parse_BoolTrue_ReturnsBool()
    {
        object? result = PatchValueParser.Parse(true);
        Assert.Equal(true, result);
    }
}
