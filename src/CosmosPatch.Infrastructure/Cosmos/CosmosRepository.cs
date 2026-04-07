using System.Net;
using CosmosPatch.Domain.Interfaces;
using CosmosPatch.Domain.Models;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json.Linq;

namespace CosmosPatch.Infrastructure.Cosmos;

/// <summary>
/// Cosmos DB implementation of IDataRepository using JObject for dynamic document access.
/// </summary>
public sealed class CosmosRepository : IDataRepository<JObject>
{
    private readonly Container _container;

    public CosmosRepository(CosmosClientManager clientManager, string databaseName, string containerName)
    {
        if (clientManager is null) throw new ArgumentNullException(nameof(clientManager));
        if (string.IsNullOrWhiteSpace(databaseName)) throw new ArgumentException("Database name is required.", nameof(databaseName));
        if (string.IsNullOrWhiteSpace(containerName)) throw new ArgumentException("Container name is required.", nameof(containerName));

        _container = clientManager.GetContainer(databaseName, containerName);
    }

    public async Task<JObject?> ReadItemAsync(string id, string partitionKey)
    {
        ItemResponse<JObject> response = await _container.ReadItemAsync<JObject>(id, new PartitionKey(partitionKey));
        return response.Resource;
    }

    public async Task<JObject> AddItemAsync(JObject item, string partitionKey)
    {
        ItemResponse<JObject> response = await _container.CreateItemAsync(item, new PartitionKey(partitionKey));
        return response.Resource;
    }

    public async Task<bool> DeleteItemAsync(string id, string partitionKey)
    {
        try
        {
            ItemResponse<JObject> response = await _container.DeleteItemAsync<JObject>(id, new PartitionKey(partitionKey));
            return response.StatusCode == HttpStatusCode.NoContent;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task<JObject?> PatchItemAsync(string id, string partitionKey, IReadOnlyList<DataPatchOperation> operations)
    {
        IReadOnlyList<PatchOperation> cosmosPatchOps = TranslatePatchOperations(operations);
        ItemResponse<JObject> response = await _container.PatchItemAsync<JObject>(id, new PartitionKey(partitionKey), cosmosPatchOps);
        return response.Resource;
    }

    public async Task<ContainerInfo> ReadContainerInfoAsync()
    {
        ContainerResponse response = await _container.ReadContainerAsync();
        string pkPath = response.Resource.PartitionKeyPath ?? string.Empty;
        return new ContainerInfo(pkPath);
    }

    private static IReadOnlyList<PatchOperation> TranslatePatchOperations(IReadOnlyList<DataPatchOperation> operations)
    {
        List<PatchOperation> result = new(operations.Count);
        foreach (DataPatchOperation op in operations)
        {
            result.Add(op.Type switch
            {
                DataPatchOperationType.Set => PatchOperation.Set(op.Path, op.Value),
                DataPatchOperationType.Remove => PatchOperation.Remove(op.Path),
                _ => throw new NotSupportedException($"Patch operation type '{op.Type}' is not supported.")
            });
        }
        return result;
    }

    public void Dispose()
    {
        // Cleanup if needed in the future
    }
}
