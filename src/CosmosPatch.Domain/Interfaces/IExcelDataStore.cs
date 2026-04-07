using System.Data;

namespace CosmosPatch.Domain.Interfaces;

/// <summary>
/// Abstraction for reading patch records from a tabular data source (e.g., Excel, CSV).
/// Also provides write-back capability to record operation results into the same source.
/// </summary>
public interface IExcelDataStore : IDisposable
{
    /// <summary>Returns the ordered column header names from the first row.</summary>
    IEnumerable<string> GetHeaders();

    /// <summary>Returns all data rows as a DataTable. Column types are inferred from row 2.</summary>
    DataTable GetDataTable();

    /// <summary>Writes a string value into the cell at the given 1-based row and column.</summary>
    void WriteCell(int row, int col, string value);

    /// <summary>Writes a string array starting at the given 1-based row and column, incrementing column.</summary>
    void WriteCells(int row, int startCol, string[] values);

    /// <summary>Persists the current state of the data store to disk.</summary>
    void Save();
}
