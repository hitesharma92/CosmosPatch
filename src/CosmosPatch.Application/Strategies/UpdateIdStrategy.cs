using System.Data;
using CosmosPatch.Domain.Constants;
using CosmosPatch.Domain.Interfaces;
using CosmosPatch.Domain.Models;
using Newtonsoft.Json.Linq;

namespace CosmosPatch.Application.Strategies;

/// <summary>
/// Changes a document's id by: backing up → deleting the old document → re-inserting with the new id.
/// Excel must have exactly 3 columns: id, partition_key, new_id.
/// </summary>
public sealed class UpdateIdStrategy : PatchStrategyBase
{
    private readonly int _deleteStatusCol = 4;
    private readonly int _patchStatusCol = 5;

    public UpdateIdStrategy(
        IDataRepository<JObject> repository,
        IExcelDataStore excelStore,
        IJsonBackupWriter backupWriter,
        IProgressReporter progressBar,
        IAppLogger logger)
        : base(repository, excelStore, backupWriter, progressBar, logger)
    {
        ValidateColumnCount();
        ValidateNewIdColumn();

        ExcelStore.WriteCell(1, _deleteStatusCol, ColumnConstants.DeleteStatus);
        ExcelStore.WriteCell(1, _patchStatusCol, ColumnConstants.PatchStatus);
    }

    public override async Task PatchAsync()
    {
        try
        {
            foreach (DataRow record in InputRecords.Rows)
            {
                int excelRow = InputRecords.Rows.IndexOf(record) + 2;
                string oldId = Convert.ToString(record[ColumnConstants.Id])!;
                string partitionKey = Convert.ToString(record[ColumnConstants.PartitionKey])!;
                string newId = Convert.ToString(record[ColumnConstants.NewId])!;

                Logger.WriteMessage($"Processing id: {oldId}, partition: {partitionKey}, new id: {newId}");

                try
                {
                    JObject? document = await Repository.ReadItemAsync(oldId, partitionKey);
                    if (document is null)
                    {
                        ExcelStore.WriteCell(excelRow, _deleteStatusCol, $"Document not found: {oldId}");
                        ProgressBar.Tick();
                        continue;
                    }

                    BackupWriter.WriteDocument(document.ToString());
                    document["id"] = newId;

                    bool deleted = await Repository.DeleteItemAsync(oldId, partitionKey);
                    if (deleted)
                    {
                        await Repository.AddItemAsync(document, partitionKey);
                        Logger.WriteMessage($"Updated id: {oldId} → {newId}");
                        ExcelStore.WriteCells(excelRow, _deleteStatusCol, new[] { ColumnConstants.Success, ColumnConstants.Success });
                    }
                    else
                    {
                        Logger.WriteMessage($"Delete failed for id: {oldId}");
                        ExcelStore.WriteCells(excelRow, _deleteStatusCol, new[] { ColumnConstants.Error, ColumnConstants.Error });
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
                $"Input Excel must have exactly 3 columns: {ColumnConstants.Id}, {ColumnConstants.PartitionKey}, {ColumnConstants.NewId}.");
    }

    private void ValidateNewIdColumn()
    {
        string? thirdCol = InputHeaders.Skip(2).FirstOrDefault();
        if (!string.Equals(thirdCol, ColumnConstants.NewId, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException(
                $"The 3rd Excel column must be named '{ColumnConstants.NewId}' but found '{thirdCol}'.");
    }

}
