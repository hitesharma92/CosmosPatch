namespace CosmosPatch.Domain.Models;

public enum DataPatchOperationType
{
    Set,
    Remove
}

/// <summary>
/// Database-agnostic representation of a patch operation.
/// The infrastructure layer translates this into the database-specific representation.
/// </summary>
public sealed class DataPatchOperation
{
    public DataPatchOperationType Type { get; }
    public string Path { get; }
    public object? Value { get; }

    private DataPatchOperation(DataPatchOperationType type, string path, object? value)
    {
        Type = type;
        Path = path;
        Value = value;
    }

    public static DataPatchOperation Set(string path, object? value)
        => new(DataPatchOperationType.Set, path, value);

    public static DataPatchOperation Remove(string path)
        => new(DataPatchOperationType.Remove, path, null);
}
