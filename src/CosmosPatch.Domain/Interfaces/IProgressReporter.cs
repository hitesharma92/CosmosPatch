namespace CosmosPatch.Domain.Interfaces;

public interface IProgressReporter : IDisposable
{
    int Max { get; set; }
    int Value { get; set; }
    void Tick(int value = 1);
}
