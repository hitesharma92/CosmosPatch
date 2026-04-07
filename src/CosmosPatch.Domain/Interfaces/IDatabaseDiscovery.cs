namespace CosmosPatch.Domain.Interfaces;

/// <summary>
/// Abstraction for discovering databases and containers from the underlying data store.
/// Enables multi-database selection flows.
/// </summary>
public interface IDatabaseDiscovery
{
    /// <summary>Returns all database names available under the configured account.</summary>
    Task<List<string>> GetDatabasesAsync();

    /// <summary>Returns all container/collection names within the specified database, sorted alphabetically.</summary>
    Task<List<string>> GetContainersAsync(string databaseName);
}
