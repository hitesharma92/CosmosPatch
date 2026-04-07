using CosmosPatch.Domain.Enums;

namespace CosmosPatch.Application.Utilities;

/// <summary>
/// Determines the deployment environment from a Cosmos endpoint URL.
/// </summary>
public static class EnvironmentDetector
{
    public static AppEnvironment Detect(string cosmosUrl)
    {
        if (string.IsNullOrWhiteSpace(cosmosUrl))
            return AppEnvironment.Unknown;

        string lower = cosmosUrl.ToLowerInvariant();

        if (lower.Contains("dev")) return AppEnvironment.DEV;
        if (lower.Contains("qa")) return AppEnvironment.QA;
        if (lower.Contains("staging")) return AppEnvironment.STAGING;
        if (lower.Contains("prod")) return AppEnvironment.PROD;

        return AppEnvironment.Unknown;
    }
}
