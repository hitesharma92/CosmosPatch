using CosmosPatch.Domain.Interfaces;

namespace CosmosPatch.Console.Logging;

/// <summary>
/// Thread-safe file logger. Writes timestamped entries to ./Logs - {name}/{timestamp} - {name}.txt
/// </summary>
public sealed class FileLogger : IAppLogger, IDisposable
{
    private StreamWriter? _writer;
    private bool _disposed;
    private readonly object _lock = new();

    public bool CreateLog(string logFileName, bool append, bool showBeginHeader)
    {
        string path = BuildFilePath(logFileName);
        _writer = new StreamWriter(path, append);
        System.Console.WriteLine($"\nLog file created at: {path}\n");

        if (showBeginHeader)
        {
            string border = new string('*', 80);
            WriteMessage(border);
            WriteMessage(append ? "Log file updated" : "Log file created");
        }
        return true;
    }

    public void CloseLog(bool showEndFooter)
    {
        if (showEndFooter) WriteMessage("Log file closed.");
        lock (_lock)
        {
            _writer?.Flush();
            _writer?.Close();
        }
    }

    public void WriteMessage(string message)
    {
        string timestamp = DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt");
        lock (_lock)
        {
            _writer?.WriteLine($"{timestamp}    {message}");
            _writer?.Flush();
        }
    }

    public void WriteMessageOnConsole(string message)
    {
        System.Console.WriteLine(message);
        WriteMessage(message);
    }

    public void BeginSection(string title) => WriteMessage($"-------- Begin {title}");

    public void EndSection(string title) => WriteMessage($"-------- End {title}");

    private static string BuildFilePath(string logFileName)
    {
        string folder = $"./Logs - {logFileName}/";
        Directory.CreateDirectory(folder);
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
        return Path.Combine(folder, $"Logs - {timestamp} - {logFileName}.txt");
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _writer?.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
