using CosmosPatch.Domain.Interfaces;
using Microsoft.Azure.Cosmos;

namespace CosmosPatch.Infrastructure.Cosmos;

/// <summary>
/// Discovers available databases and containers from the Cosmos account.
/// Enables the multi-database selection flow at application startup.
/// </summary>
public sealed class CosmosDatabaseDiscovery : IDatabaseDiscovery
{
    private readonly CosmosClientManager _clientManager;

    public CosmosDatabaseDiscovery(CosmosClientManager clientManager)
    {
        _clientManager = clientManager ?? throw new ArgumentNullException(nameof(clientManager));
    }

    public async Task<List<string>> GetDatabasesAsync()
    {
        List<string> databases = new();
        FeedIterator<DatabaseProperties> iterator = _clientManager.GetClient()
            .GetDatabaseQueryIterator<DatabaseProperties>();

        while (iterator.HasMoreResults)
        {
            FeedResponse<DatabaseProperties> response = await iterator.ReadNextAsync();
            foreach (DatabaseProperties db in response)
            {
                databases.Add(db.Id);
            }
        }

        return databases.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public async Task<List<string>> GetContainersAsync(string databaseName)
    {
        if (string.IsNullOrWhiteSpace(databaseName))
            throw new ArgumentException("Database name is required.", nameof(databaseName));

        List<string> containers = new();
        Database database = _clientManager.GetClient().GetDatabase(databaseName);
        FeedIterator<ContainerProperties> iterator = database.GetContainerQueryIterator<ContainerProperties>();

        while (iterator.HasMoreResults)
        {
            FeedResponse<ContainerProperties> response = await iterator.ReadNextAsync();
            foreach (ContainerProperties container in response)
            {
                containers.Add(container.Id);
            }
        }

        return containers.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
    }
}
