using System.Data;
using CosmosPatch.Domain.Constants;
using CosmosPatch.Domain.Interfaces;
using CosmosPatch.Domain.Models;
using Newtonsoft.Json.Linq;

namespace CosmosPatch.Application.Strategies;

/// <summary>
/// Changes a document's partition key by: backing up → deleting the old document → re-inserting with the new partition key.
/// Excel must have exactly 3 columns: id, partition_key, {actualPartitionKeyPropertyName}.
/// </summary>
public sealed class UpdatePartitionKeyStrategy : PatchStrategyBase
{
    private readonly string _partitionKeyProperty;
    private readonly int _deleteStatusCol = 4;
    private readonly int _patchStatusCol = 5;

    public UpdatePartitionKeyStrategy(
        IDataRepository<JObject> repository,
        IExcelDataStore excelStore,
        IJsonBackupWriter backupWriter,
        IProgressReporter progressBar,
        IAppLogger logger)
        : base(repository, excelStore, backupWriter, progressBar, logger)
    {
        ContainerInfo containerInfo = Repository.ReadContainerInfoAsync().GetAwaiter().GetResult();
        _partitionKeyProperty = containerInfo.PartitionKeyPath;

        ValidateColumnCount();
        ValidateThirdColumnMatchesPartitionKey();

        ExcelStore.WriteCell(1, _deleteStatusCol, ColumnConstants.DeleteStatus);
        ExcelStore.WriteCell(1, _patchStatusCol, ColumnConstants.PatchStatus);
    }

    public override async Task PatchAsync()
    {
        try
        {
            List<Task> deleteTasks = new();
            List<Task> createTasks = new();

            foreach (DataRow record in InputRecords.Rows)
            {
                int excelRow = InputRecords.Rows.IndexOf(record) + 2;
                string oldId = Convert.ToString(record[ColumnConstants.Id])!;
                string oldPartitionValue = Convert.ToString(record[ColumnConstants.PartitionKey])!;
                string newPartitionValue = Convert.ToString(record[_partitionKeyProperty])!;

                Logger.WriteMessage($"Processing id: {oldId}, old partition: {oldPartitionValue} → new: {newPartitionValue}");

                try
                {
                    JObject? document = await Repository.ReadItemAsync(oldId, oldPartitionValue);
                    if (document is not null)
                    {
                        BackupWriter.WriteDocument(document.ToString());
                        document[_partitionKeyProperty] = newPartitionValue;

                        deleteTasks.Add(Repository.DeleteItemAsync(oldId, oldPartitionValue)
                            .ContinueWith(deleteTask =>
                            {
                                if (deleteTask.Result)
                                {
                                    createTasks.Add(Repository.AddItemAsync(document, newPartitionValue)
                                        .ContinueWith(_ =>
                                        {
                                            Logger.WriteMessage($"Updated partition: {oldPartitionValue} → {newPartitionValue} for id: {oldId}");
                                            ExcelStore.WriteCells(excelRow, _deleteStatusCol, new[] { ColumnConstants.Success, ColumnConstants.Success });
                                        }));
                                }
                                else
                                {
                                    Logger.WriteMessage($"Delete failed for id: {oldId}");
                                    ExcelStore.WriteCells(excelRow, _deleteStatusCol, new[] { ColumnConstants.Error, ColumnConstants.Error });
                                }
                            }));
                    }
                    else
                    {
                        ExcelStore.WriteCell(excelRow, _deleteStatusCol, $"Document not found: {oldId}");
                    }
                }
                catch (Exception ex) when (IsCosmosException(ex))
                {
                    ExcelStore.WriteCell(excelRow, _deleteStatusCol, ex.Message);
                    Logger.WriteMessage($"Database exception for id: {oldId}. Message: {ex.Message}");
                }

                if (excelRow % 50 == 0) ExcelStore.Save();
                ProgressBar.Tick();
            }

            await Task.WhenAll(deleteTasks);
            await Task.WhenAll(createTasks);
            ExcelStore.Save();
        }
        finally
        {
            BackupWriter.Close();
            ExcelStore.Save();
            ProgressBar.Dispose();
        }
    }

    private void ValidateColumnCount()
    {
        if (InputHeaders.Count() != 3)
            throw new ArgumentException(
                "Input Excel must have exactly 3 columns: id, partition_key, and the new partition key column.");
    }

    private void ValidateThirdColumnMatchesPartitionKey()
    {
        string? thirdCol = InputHeaders.Skip(2).FirstOrDefault();
        if (thirdCol != _partitionKeyProperty)
            throw new ArgumentException(
                $"The 3rd Excel column '{thirdCol}' does not match the container's partition key property '{_partitionKeyProperty}'.");
    }

    private static bool IsCosmosException(Exception ex)
        => ex.GetType().Name.Contains("CosmosException", StringComparison.OrdinalIgnoreCase);
}
