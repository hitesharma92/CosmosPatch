using System.Data;
using ClosedXML.Excel;
using CosmosPatch.Domain.Interfaces;

namespace CosmosPatch.Infrastructure.Storage.Excel;

/// <summary>
/// ClosedXML-based implementation of IExcelDataStore.
/// Opens an existing Excel file, reads its data as a DataTable,
/// and supports writing result values back to the same file.
/// </summary>
public sealed class ExcelDataStore : IExcelDataStore
{
    private readonly string _filePath;
    private XLWorkbook _workbook;
    private IXLWorksheet _worksheet;
    private bool _disposed;

    public ExcelDataStore(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path is required.", nameof(filePath));
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Excel file not found at: {filePath}", filePath);

        _filePath = filePath;
        _workbook = new XLWorkbook(filePath);
        _worksheet = _workbook.Worksheet(1);
    }

    public IEnumerable<string> GetHeaders()
    {
        int lastCol = _worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;
        List<string> headers = new();
        for (int col = 1; col <= lastCol; col++)
        {
            headers.Add(_worksheet.Cell(1, col).GetString());
        }
        return headers;
    }

    public DataTable GetDataTable()
    {
        return ReadDataTable(_filePath);
    }

    public void WriteCell(int row, int col, string value)
    {
        _worksheet.Cell(row, col).Value = value;
    }

    public void WriteCells(int row, int startCol, string[] values)
    {
        for (int i = 0; i < values.Length; i++)
        {
            _worksheet.Cell(row, startCol + i).Value = values[i];
        }
    }

    public void Save()
    {
        _workbook.SaveAs(_filePath);
    }

    // ── Static helper: reads DataTable from a separate stream (used in base class) ──

    private static DataTable ReadDataTable(string path)
    {
        using XLWorkbook wb = new(path);
        IXLWorksheet ws = wb.Worksheet(1);

        TrimTrailingEmptyRows(ws);

        int lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;
        int lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 0;

        DataTable table = new();

        // Build columns from row 1
        Dictionary<int, Type> colTypes = new();
        for (int col = 1; col <= lastCol; col++)
        {
            string header = ws.Cell(1, col).GetString();
            Type colType = InferColumnType(ws, col, lastRow);
            colTypes[col] = colType;
            table.Columns.Add(header, colType);
        }

        // Read data rows from row 2 onwards
        for (int row = 2; row <= lastRow; row++)
        {
            DataRow dataRow = table.NewRow();
            for (int col = 1; col <= lastCol; col++)
            {
                IXLCell cell = ws.Cell(row, col);
                dataRow[col - 1] = ParseCellValue(cell, colTypes[col]);
            }
            table.Rows.Add(dataRow);
        }

        return table;
    }

    private static Type InferColumnType(IXLWorksheet ws, int col, int lastRow)
    {
        // Inspect row 2 of this column to infer type
        for (int row = 2; row <= lastRow; row++)
        {
            IXLCell cell = ws.Cell(row, col);
            string text = cell.GetString();

            if (string.IsNullOrWhiteSpace(text)) continue;

            if ((text.StartsWith("{") && text.EndsWith("}")) ||
                (text.StartsWith("[") && text.EndsWith("]")))
                return typeof(object);

            XLDataType dataType = cell.DataType;
            if (dataType == XLDataType.DateTime) return typeof(DateTime);
            if (dataType == XLDataType.Boolean) return typeof(bool);
            if (dataType == XLDataType.Number)
            {
                if (cell.Value.IsNumber)
                {
                    double d = (double)cell.Value;
                    if (d == Math.Floor(d) && !double.IsInfinity(d))
                        return typeof(long);
                    return typeof(double);
                }
            }
            return typeof(string);
        }

        return typeof(string);
    }

    private static object ParseCellValue(IXLCell cell, Type targetType)
    {
        string text = cell.GetString();

        if (string.IsNullOrWhiteSpace(text))
            return DBNull.Value;

        if (targetType == typeof(bool))
        {
            if (text == "1" || text.Equals("true", StringComparison.OrdinalIgnoreCase)) return true;
            if (text == "0" || text.Equals("false", StringComparison.OrdinalIgnoreCase)) return false;
            return DBNull.Value;
        }

        if (targetType == typeof(long))
        {
            return long.TryParse(text, out long l) ? l : (object)DBNull.Value;
        }

        if (targetType == typeof(double))
        {
            return double.TryParse(text, out double d) ? d : (object)DBNull.Value;
        }

        if (targetType == typeof(DateTime))
        {
            if (cell.DataType == XLDataType.DateTime)
                return cell.GetDateTime();
            return DateTime.TryParse(text, out DateTime dt) ? dt : (object)DBNull.Value;
        }

        // object or string — return the text as-is
        return text;
    }

    private static void TrimTrailingEmptyRows(IXLWorksheet ws)
    {
        while (true)
        {
            IXLRow? lastRow = ws.LastRowUsed();
            if (lastRow is null) break;

            bool isEmpty = lastRow.Cells().All(c => c.IsEmpty());
            if (!isEmpty) break;

            lastRow.Delete();
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _workbook?.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
