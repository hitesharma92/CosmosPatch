namespace CosmosPatch.Domain.Interfaces;

public interface IJsonBackupWriter : IDisposable
{
    void WriteDocument(string jsonContent);
    void Close();
}
