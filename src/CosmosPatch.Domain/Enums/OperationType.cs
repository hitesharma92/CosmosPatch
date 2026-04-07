namespace CosmosPatch.Domain.Enums;

public enum OperationType
{
    PatchProperty = 1,
    UpdatePartitionKey = 2,
    RemoveChildArrayItem = 3,
    PatchChildArrayItem = 4,
    UpdateId = 5
}
