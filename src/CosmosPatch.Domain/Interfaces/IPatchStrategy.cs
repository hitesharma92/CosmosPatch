namespace CosmosPatch.Domain.Interfaces;

/// <summary>
/// Abstraction for a patch strategy. Each operation type (PatchProperty, UpdateId, etc.) is a strategy.
/// </summary>
public interface IPatchStrategy
{
    Task PatchAsync();
}
