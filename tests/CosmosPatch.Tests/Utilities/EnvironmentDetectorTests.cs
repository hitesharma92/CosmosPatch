using CosmosPatch.Application.Utilities;
using CosmosPatch.Domain.Enums;

namespace CosmosPatch.Tests.Utilities;

public class EnvironmentDetectorTests
{
    [Theory]
    [InlineData("https://myaccount-dev.documents.azure.com:443/", AppEnvironment.DEV)]
    [InlineData("https://myaccount-qa.documents.azure.com:443/", AppEnvironment.QA)]
    [InlineData("https://myaccount-staging.documents.azure.com:443/", AppEnvironment.STAGING)]
    [InlineData("https://myaccount-prod.documents.azure.com:443/", AppEnvironment.PROD)]
    [InlineData("https://myaccount.documents.azure.com:443/", AppEnvironment.Unknown)]
    [InlineData("", AppEnvironment.Unknown)]
    [InlineData(null, AppEnvironment.Unknown)]
    public void Detect_ReturnsExpectedEnvironment(string? url, AppEnvironment expected)
    {
        AppEnvironment result = EnvironmentDetector.Detect(url!);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Detect_CaseInsensitive_DevInUpperCase()
    {
        // The URL itself is lowercased before matching
        string url = "https://MYACCOUNT-DEV.documents.azure.com/";
        AppEnvironment result = EnvironmentDetector.Detect(url);
        Assert.Equal(AppEnvironment.DEV, result);
    }
}
