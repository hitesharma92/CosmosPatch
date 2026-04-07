using CosmosPatch.Domain.Models;

namespace CosmosPatch.Domain.Interfaces;

/// <summary>
/// Database-agnostic data access contract for a single container/collection.
/// The generic parameter T is the document type (typically JObject for dynamic operations).
/// </summary>
public interface IDataRepository<T> : IDisposable where T : class
{
    Task<T?> ReadItemAsync(string id, string partitionKey);

    Task<T> AddItemAsync(T item, string partitionKey);

    Task<bool> DeleteItemAsync(string id, string partitionKey);

    Task<T?> PatchItemAsync(string id, string partitionKey, IReadOnlyList<DataPatchOperation> operations);

    Task<ContainerInfo> ReadContainerInfoAsync();
}
