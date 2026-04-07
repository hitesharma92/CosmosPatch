using ClosedXML.Excel;
using CosmosPatch.Infrastructure.Storage.Excel;

namespace CosmosPatch.Tests.Excel;

public class ExcelDataStoreTests : IDisposable
{
    private readonly string _testFilePath;

    public ExcelDataStoreTests()
    {
        _testFilePath = Path.Combine(Path.GetTempPath(), $"cosmos_patch_test_{Guid.NewGuid()}.xlsx");
        CreateTestExcel(_testFilePath);
    }

    private static void CreateTestExcel(string path)
    {
        using XLWorkbook wb = new();
        IXLWorksheet ws = wb.AddWorksheet("Sheet1");
        // Headers
        ws.Cell(1, 1).Value = "id";
        ws.Cell(1, 2).Value = "partition_key";
        ws.Cell(1, 3).Value = "name";
        ws.Cell(1, 4).Value = "age";
        // Data row 1
        ws.Cell(2, 1).Value = "doc-001";
        ws.Cell(2, 2).Value = "partition-a";
        ws.Cell(2, 3).Value = "Alice";
        ws.Cell(2, 4).Value = 30;
        // Data row 2
        ws.Cell(3, 1).Value = "doc-002";
        ws.Cell(3, 2).Value = "partition-b";
        ws.Cell(3, 3).Value = "Bob";
        ws.Cell(3, 4).Value = 25;
        wb.SaveAs(path);
    }

    [Fact]
    public void GetHeaders_ReturnsAllHeaders()
    {
        using ExcelDataStore store = new(_testFilePath);
        IEnumerable<string> headers = store.GetHeaders();
        Assert.Equal(new[] { "id", "partition_key", "name", "age" }, headers);
    }

    [Fact]
    public void GetDataTable_ReturnsCorrectRowCount()
    {
        using ExcelDataStore store = new(_testFilePath);
        System.Data.DataTable table = store.GetDataTable();
        Assert.Equal(2, table.Rows.Count);
    }

    [Fact]
    public void GetDataTable_ReturnsCorrectValues()
    {
        using ExcelDataStore store = new(_testFilePath);
        System.Data.DataTable table = store.GetDataTable();
        Assert.Equal("doc-001", table.Rows[0]["id"]);
        Assert.Equal("Alice", table.Rows[0]["name"]);
    }

    [Fact]
    public void WriteCell_UpdatesCellValue()
    {
        using ExcelDataStore store = new(_testFilePath);
        store.WriteCell(2, 3, "UpdatedName");
        store.Save();

        // Reload and verify
        using ExcelDataStore reloaded = new(_testFilePath);
        System.Data.DataTable table = reloaded.GetDataTable();
        Assert.Equal("UpdatedName", table.Rows[0]["name"]);
    }

    [Fact]
    public void Constructor_ThrowsFileNotFoundException_ForMissingFile()
    {
        Assert.Throws<FileNotFoundException>(() =>
            new ExcelDataStore("C:\\nonexistent\\file.xlsx"));
    }

    public void Dispose()
    {
        if (File.Exists(_testFilePath)) File.Delete(_testFilePath);
    }
}
