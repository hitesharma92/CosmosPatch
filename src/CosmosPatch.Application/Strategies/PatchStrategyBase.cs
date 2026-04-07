using System.Data;
using CosmosPatch.Domain.Constants;
using CosmosPatch.Domain.Interfaces;
using CosmosPatch.Domain.Models;
using Newtonsoft.Json.Linq;

namespace CosmosPatch.Application.Strategies;

/// <summary>
/// Abstract base for all patch strategies.
/// Handles common initialization: Excel validation, DataTable loading, progress bar setup.
/// </summary>
public abstract class PatchStrategyBase : IPatchStrategy
{
    protected readonly IAppLogger Logger;
    protected readonly IDataRepository<JObject> Repository;
    protected readonly IExcelDataStore ExcelStore;
    protected readonly IJsonBackupWriter BackupWriter;
    protected readonly IProgressReporter ProgressBar;

    protected readonly IEnumerable<string> InputHeaders;
    protected readonly DataTable InputRecords;

    protected PatchStrategyBase(
        IDataRepository<JObject> repository,
        IExcelDataStore excelStore,
        IJsonBackupWriter backupWriter,
        IProgressReporter progressBar,
        IAppLogger logger)
    {
        Repository = repository ?? throw new ArgumentNullException(nameof(repository));
        ExcelStore = excelStore ?? throw new ArgumentNullException(nameof(excelStore));
        BackupWriter = backupWriter ?? throw new ArgumentNullException(nameof(backupWriter));
        ProgressBar = progressBar ?? throw new ArgumentNullException(nameof(progressBar));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));

        InputHeaders = ExcelStore.GetHeaders();
        ValidateRequiredHeaders();

        InputRecords = ExcelStore.GetDataTable();
        ValidateRecordCount();

        ProgressBar.Max = InputRecords.Rows.Count;

        Logger.WriteMessageOnConsole($"Found {InputRecords.Rows.Count} records in input Excel.");
    }

    public abstract Task PatchAsync();

    // ── Column helpers ──

    /// <summary>
    /// Returns a dictionary where key = column header containing <paramref name="prefix"/>
    /// and value = the column name with the prefix stripped.
    /// Skips the first two columns (id, partition_key).
    /// </summary>
    protected Dictionary<string, string> GetPrefixedColumns(string prefix)
    {
        Dictionary<string, string> result = new(StringComparer.OrdinalIgnoreCase);
        foreach (string col in InputHeaders.Skip(2).Where(h => h.Contains(prefix, StringComparison.OrdinalIgnoreCase)))
        {
            result[col] = col.Replace(prefix, string.Empty, StringComparison.OrdinalIgnoreCase);
        }
        return result;
    }

    // ── Validation ──

    private void ValidateRequiredHeaders()
    {
        Logger.WriteMessageOnConsole($"Validating required columns ('{ColumnConstants.Id}', '{ColumnConstants.PartitionKey}') in Excel input.");

        if (InputHeaders.FirstOrDefault() != ColumnConstants.Id)
            throw new ArgumentException($"'{ColumnConstants.Id}' column is missing from first Excel column.");

        if (InputHeaders.Skip(1).FirstOrDefault() != ColumnConstants.PartitionKey)
            throw new ArgumentException($"'{ColumnConstants.PartitionKey}' column is missing from second Excel column.");
    }

    private void ValidateRecordCount()
    {
        if (InputRecords is null || InputRecords.Rows.Count < 1)
            throw new ArgumentException("No data rows found in the Excel input file.");
    }
}
