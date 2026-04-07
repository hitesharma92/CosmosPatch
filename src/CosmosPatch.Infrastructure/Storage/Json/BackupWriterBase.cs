namespace CosmosPatch.Infrastructure.Storage.Json;

/// <summary>
/// Base class that creates the backup folder/file and owns the StreamWriter.
/// </summary>
public abstract class BackupWriterBase : IDisposable
{
    protected readonly StreamWriter StreamWriter;
    private bool _disposed;

    protected BackupWriterBase(string folderPath, string backupFileName, bool append, string extension)
    {
        string filePath = BuildFilePath(folderPath, backupFileName, extension);
        StreamWriter = new StreamWriter(filePath, append);
    }

    private static string BuildFilePath(string folderPath, string backupFileName, string extension)
    {
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
        string fileName = $"Backup - {backupFileName} - {timestamp}.{extension}";
        Directory.CreateDirectory(folderPath);
        return Path.Combine(folderPath, fileName);
    }

    protected void CloseWriter()
    {
        StreamWriter.Flush();
        StreamWriter.Close();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            StreamWriter?.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
