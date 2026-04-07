namespace CosmosPatch.Console.IO;

/// <summary>
/// All console prompts for collecting user input at startup.
/// </summary>
public static class ConsoleInputReader
{
    public static string GetCosmosEndpointUrl()
        => Prompt("\nPlease enter the Cosmos DB Endpoint URL (starts with https://): ");

    public static string GetCosmosAccountKey()
        => Prompt("\nPlease enter the Cosmos DB Read/Write Account Key: ");

    public static string GetInputExcelFilePath()
        => Prompt("\nEnter the full path to the input Excel file (e.g., C:\\Data\\input.xlsx): ");

    public static string Prompt(string message)
    {
        System.Console.WriteLine(message);
        return System.Console.ReadLine() ?? string.Empty;
    }

    public static bool VerifyFilePath(string path)
    {
        if (File.Exists(path))
        {
            System.Console.WriteLine($"File verified: {path}");
            return true;
        }
        throw new FileNotFoundException($"Input file not found at path: {path}", path);
    }
}
