using System.Data;
using CosmosPatch.Application.Utilities;
using CosmosPatch.Domain.Constants;
using CosmosPatch.Domain.Interfaces;
using CosmosPatch.Domain.Models;
using Newtonsoft.Json.Linq;

namespace CosmosPatch.Application.Strategies;

/// <summary>
/// Sets one or more document properties using the Excel columns (after id/partition_key) as patch targets.
/// </summary>
public sealed class PatchPropertyStrategy : PatchStrategyBase
{
    private readonly List<string> _patchColumns;
    private readonly int _statusColumnIndex;

    public PatchPropertyStrategy(
        IDataRepository<JObject> repository,
        IExcelDataStore excelStore,
        IJsonBackupWriter backupWriter,
        IProgressReporter progressBar,
        IAppLogger logger)
        : base(repository, excelStore, backupWriter, progressBar, logger)
    {
        _patchColumns = InputHeaders.Skip(2).ToList();
        ValidateAndConfirmPatchColumns();

        _statusColumnIndex = InputHeaders.Count() + 1;
        ExcelStore.WriteCell(1, _statusColumnIndex, ColumnConstants.PatchStatus);
    }

    public override async Task PatchAsync()
    {
        try
        {
            int excelRow = 2;
            foreach (DataRow record in InputRecords.Rows)
            {
                string id = Convert.ToString(record[ColumnConstants.Id])!;
                string partitionKey = Convert.ToString(record[ColumnConstants.PartitionKey])!;
                Logger.WriteMessage($"Processing id: {id}, partition: {partitionKey}");

                List<DataPatchOperation> ops = new();
                foreach (string col in _patchColumns)
                {
                    object? value = PatchValueParser.Parse(record[col]);
                    ops.Add(DataPatchOperation.Set($"/{col}", value));
                }

                try
                {
                    JObject? result = await Repository.PatchItemAsync(id, partitionKey, ops);
                    if (result is not null)
                    {
                        BackupWriter.WriteDocument(result.ToString());
                        ExcelStore.WriteCell(excelRow, _statusColumnIndex, ColumnConstants.Success);
                        Logger.WriteMessage($"Successfully patched id: {id}");
                    }
                    else
                    {
                        ExcelStore.WriteCell(excelRow, _statusColumnIndex, ColumnConstants.Error);
                        Logger.WriteMessage($"Patch returned null for id: {id}");
                    }
                }
                catch (Exception ex) when (IsCosmosException(ex))
                {
                    ExcelStore.WriteCell(excelRow, _statusColumnIndex, ex.Message);
                    Logger.WriteMessage($"Database exception for id: {id}. Message: {ex.Message}");
                }

                excelRow++;
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

    private void ValidateAndConfirmPatchColumns()
    {
        Logger.WriteMessageOnConsole("\nColumns to be patched (values come from the corresponding Excel column):");
        foreach (string col in _patchColumns)
            Logger.WriteMessageOnConsole($"  {col}");

        if (!ConfirmWithUser())
            throw new OperationCanceledException("User cancelled the operation after reviewing column names.");
    }

    private static bool ConfirmWithUser()
    {
        Console.Write("\nProceed with these columns? (y/n): ");
        string? input = Console.ReadLine();
        return !string.IsNullOrWhiteSpace(input) && input.Equals("y", StringComparison.OrdinalIgnoreCase);
    }

}
