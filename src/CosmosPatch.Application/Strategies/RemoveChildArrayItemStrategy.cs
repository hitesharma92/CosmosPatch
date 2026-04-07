using System.Data;
using CosmosPatch.Application.Utilities;
using CosmosPatch.Domain.Constants;
using CosmosPatch.Domain.Interfaces;
using CosmosPatch.Domain.Models;
using Newtonsoft.Json.Linq;

namespace CosmosPatch.Application.Strategies;

/// <summary>
/// Removes an element from a child array by matching WHERE-clause column values.
/// Excel columns after id/partition_key are treated as WHERE filters for the child array item.
/// The array property name is prompted from the user at construction time.
/// </summary>
public sealed class RemoveChildArrayItemStrategy : PatchStrategyBase
{
    private readonly string _arrayPropertyName;
    private readonly List<string> _whereColumns;
    private readonly int _statusColumnIndex;

    public RemoveChildArrayItemStrategy(
        IDataRepository<JObject> repository,
        IExcelDataStore excelStore,
        IJsonBackupWriter backupWriter,
        IProgressReporter progressBar,
        IAppLogger logger)
        : base(repository, excelStore, backupWriter, progressBar, logger)
    {
        _arrayPropertyName = PromptArrayPropertyName();
        _whereColumns = InputHeaders.Skip(2).ToList();
        ValidateAndConfirmWhereColumns();

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

                try
                {
                    JObject? document = await Repository.ReadItemAsync(id, partitionKey);
                    if (document is null)
                    {
                        ExcelStore.WriteCell(excelRow, _statusColumnIndex, $"Document not found: {id}");
                        continue;
                    }

                    BackupWriter.WriteDocument(document.ToString());

                    Dictionary<string, object> whereCriteria = BuildWhereCriteria(record);
                    JArray? childArray = document[_arrayPropertyName] as JArray;

                    if (childArray is null)
                    {
                        ExcelStore.WriteCell(excelRow, _statusColumnIndex, $"Array '{_arrayPropertyName}' not found");
                        Logger.WriteMessage($"Array '{_arrayPropertyName}' not found in document id: {id}");
                        continue;
                    }

                    int index = ArrayItemMatcher.GetIndexOfMatchingObject(childArray, whereCriteria);
                    if (index == -1)
                    {
                        ExcelStore.WriteCell(excelRow, _statusColumnIndex, ColumnConstants.Error);
                        Logger.WriteMessage($"No matching array item found for id: {id}");
                        continue;
                    }

                    List<DataPatchOperation> ops = new()
                    {
                        DataPatchOperation.Remove($"/{_arrayPropertyName}/{index}")
                    };

                    JObject? result = await Repository.PatchItemAsync(id, partitionKey, ops);
                    ExcelStore.WriteCell(excelRow, _statusColumnIndex,
                        result is not null ? ColumnConstants.Success : ColumnConstants.Error);
                    Logger.WriteMessage(result is not null ? $"Successfully removed array item for id: {id}" : $"Patch returned null for id: {id}");
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

    private Dictionary<string, object> BuildWhereCriteria(DataRow record)
    {
        Dictionary<string, object> criteria = new();
        foreach (string col in _whereColumns)
        {
            criteria[col] = record[col];
        }
        return criteria;
    }

    private void ValidateAndConfirmWhereColumns()
    {
        Logger.WriteMessageOnConsole("\nWHERE-clause columns (used to find the matching array item):");
        foreach (string col in _whereColumns)
            Logger.WriteMessageOnConsole($"  {col}");

        if (!ConfirmWithUser())
            throw new OperationCanceledException("User cancelled the operation after reviewing WHERE columns.");
    }

    private static string PromptArrayPropertyName()
    {
        Console.Write("\n\nEnter the name of the child array property (e.g., orders, subgroups): ");
        string? input = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("Array property name is required.");
        return input.Trim();
    }

    private static bool ConfirmWithUser()
    {
        Console.Write("\nProceed with these WHERE columns? (y/n): ");
        string? input = Console.ReadLine();
        return !string.IsNullOrWhiteSpace(input) && input.Equals("y", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCosmosException(Exception ex)
        => ex.GetType().Name.Contains("CosmosException", StringComparison.OrdinalIgnoreCase);
}
