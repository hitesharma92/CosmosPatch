namespace CosmosPatch.Domain.Models;

/// <summary>
/// Represents metadata about a database container relevant to patch operations.
/// </summary>
public sealed class ContainerInfo
{
    public string PartitionKeyPath { get; }

    public ContainerInfo(string partitionKeyPath)
    {
        PartitionKeyPath = partitionKeyPath?.TrimStart('/') ?? string.Empty;
    }
}
