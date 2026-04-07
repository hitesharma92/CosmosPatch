using System.Data;
using CosmosPatch.Application.Utilities;
using CosmosPatch.Domain.Constants;
using CosmosPatch.Domain.Interfaces;
using CosmosPatch.Domain.Models;
using Newtonsoft.Json.Linq;

namespace CosmosPatch.Application.Strategies;

/// <summary>
/// Patches specific properties within a child array item.
/// Excel columns must use "where:{property}" and "patch:{property}" prefixes.
/// The array property name is prompted from the user at construction time.
///
/// Example Excel format:
/// id | partition_key | where:order_name | where:sub_group | patch:username | patch:phone
/// </summary>
public sealed class PatchChildArrayItemStrategy : PatchStrategyBase
{
    private readonly string _arrayPropertyName;
    private readonly Dictionary<string, string> _patchColumns;    // key=patch:prop, value=prop
    private readonly Dictionary<string, string> _whereColumns;    // key=where:prop, value=prop
    private readonly int _statusColumnIndex;

    public PatchChildArrayItemStrategy(
        IDataRepository<JObject> repository,
        IExcelDataStore excelStore,
        IJsonBackupWriter backupWriter,
        IProgressReporter progressBar,
        IAppLogger logger)
        : base(repository, excelStore, backupWriter, progressBar, logger)
    {
        ShowExcelFormatGuidance();
        _arrayPropertyName = PromptArrayPropertyName();
        _patchColumns = GetPrefixedColumns(ColumnConstants.PatchColumnPrefix);
        _whereColumns = GetPrefixedColumns(ColumnConstants.WhereClausePrefix);

        ValidateAndConfirmColumns("WHERE", _whereColumns.Values);
        ValidateAndConfirmColumns("PATCH", _patchColumns.Values);

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

                    List<DataPatchOperation> ops = new();
                    foreach (KeyValuePair<string, string> patchCol in _patchColumns)
                    {
                        // patchCol.Key = "patch:username", patchCol.Value = "username"
                        object? value = PatchValueParser.Parse(record[patchCol.Key]);
                        ops.Add(DataPatchOperation.Set($"/{_arrayPropertyName}/{index}/{patchCol.Value}", value));
                    }

                    JObject? result = await Repository.PatchItemAsync(id, partitionKey, ops);
                    ExcelStore.WriteCell(excelRow, _statusColumnIndex,
                        result is not null ? ColumnConstants.Success : ColumnConstants.Error);
                    Logger.WriteMessage(result is not null ? $"Successfully patched array item for id: {id}" : $"Patch returned null for id: {id}");
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
        foreach (KeyValuePair<string, string> kv in _whereColumns)
        {
            // kv.Key = "where:order_name", kv.Value = "order_name"
            criteria[kv.Value] = record[kv.Key];
        }
        return criteria;
    }

    private void ValidateAndConfirmColumns(string label, IEnumerable<string> columns)
    {
        Logger.WriteMessageOnConsole($"\n{label} columns:");
        foreach (string col in columns)
            Logger.WriteMessageOnConsole($"  {col}");

        Console.Write($"\nConfirm {label} columns? (y/n): ");
        string? input = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(input) || !input.Equals("y", StringComparison.OrdinalIgnoreCase))
            throw new OperationCanceledException($"User cancelled after reviewing {label} columns.");
    }

    private static void ShowExcelFormatGuidance()
    {
        Console.WriteLine("\nExpected Excel column format for this operation:");
        Console.WriteLine("  id | partition_key | where:{property} | ... | patch:{property} | ...");
        Console.WriteLine("  Example:");
        Console.WriteLine("  id | partition_key | where:order_name | patch:username | patch:phone\n");
    }

    private static string PromptArrayPropertyName()
    {
        Console.Write("\nEnter the name of the child array property (e.g., orders, subgroups): ");
        string? input = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("Array property name is required.");
        return input.Trim();
    }

    private static bool IsCosmosException(Exception ex)
        => ex.GetType().Name.Contains("CosmosException", StringComparison.OrdinalIgnoreCase);
}
