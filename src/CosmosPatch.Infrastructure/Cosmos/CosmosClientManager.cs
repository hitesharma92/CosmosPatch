using CosmosPatch.Domain.Interfaces;
using CosmosPatch.Domain.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;
using Newtonsoft.Json.Linq;

namespace CosmosPatch.Infrastructure.Cosmos;

/// <summary>
/// Manages the CosmosClient lifecycle. DI-registered as a singleton per application lifetime.
/// Not a static singleton — injectable and replaceable for testing.
/// </summary>
public sealed class CosmosClientManager : IDisposable
{
    private CosmosClient? _cosmosClient;
    private bool _disposed;

    public void Initialize(string accountEndpoint, string accountKey)
    {
        if (string.IsNullOrWhiteSpace(accountEndpoint))
            throw new ArgumentException("Cosmos account endpoint is required.", nameof(accountEndpoint));
        if (string.IsNullOrWhiteSpace(accountKey))
            throw new ArgumentException("Cosmos account key is required.", nameof(accountKey));

        CosmosClientBuilder builder = new CosmosClientBuilder(accountEndpoint, accountKey);
        _cosmosClient = builder
            .WithConnectionModeGateway()
            .WithBulkExecution(true)
            .Build();
    }

    public Container GetContainer(string databaseName, string containerName)
    {
        EnsureInitialized();
        return _cosmosClient!.GetContainer(databaseName, containerName);
    }

    public CosmosClient GetClient()
    {
        EnsureInitialized();
        return _cosmosClient!;
    }

    private void EnsureInitialized()
    {
        if (_cosmosClient is null)
            throw new InvalidOperationException(
                "CosmosClientManager has not been initialized. Call Initialize(endpoint, key) first.");
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _cosmosClient?.Dispose();
            _disposed = true;
        }
    }
}
