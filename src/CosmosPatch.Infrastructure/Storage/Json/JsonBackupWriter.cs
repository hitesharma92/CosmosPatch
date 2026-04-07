using CosmosPatch.Domain.Constants;
using CosmosPatch.Domain.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CosmosPatch.Infrastructure.Storage.Json;

/// <summary>
/// Writes Cosmos documents as a JSON array to a timestamped backup file.
/// Format: [ {doc1}, {doc2}, ]
/// </summary>
public sealed class JsonBackupWriter : BackupWriterBase, IJsonBackupWriter
{
    public JsonBackupWriter(string backupName, bool append)
        : base($"./Backup - {backupName}/", backupName, append, ColumnConstants.JsonExtension)
    {
        StreamWriter.Write("[");
    }

    public void WriteDocument(string jsonContent)
    {
        StreamWriter.WriteLine(jsonContent + ",");
        StreamWriter.Flush();
    }

    public void WriteDocument(JObject document)
    {
        string serialised = JObject.Parse(JsonConvert.SerializeObject(document)).ToString(Formatting.None);
        WriteDocument(serialised);
    }

    public void Close()
    {
        StreamWriter.WriteLine("]");
        CloseWriter();
    }
}
