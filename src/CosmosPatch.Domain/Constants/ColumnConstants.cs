namespace CosmosPatch.Domain.Constants;

public static class ColumnConstants
{
    public const string Id = "id";
    public const string PartitionKey = "partition_key";
    public const string NewId = "new_id";
    public const string PatchStatus = "PatchStatus";
    public const string DeleteStatus = "Delete";
    public const string WhereClausePrefix = "where:";
    public const string PatchColumnPrefix = "patch:";
    public const string Success = "Success";
    public const string Error = "Error";
    public const string JsonExtension = "json";
    public const int MaxItemsPerCosmosPage = 2000;
}
