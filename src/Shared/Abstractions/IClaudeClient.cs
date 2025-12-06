namespace Shared.Abstractions;

public interface IClaudeClient
{
    Task<string> CompleteAsync(string prompt, CancellationToken ct = default);
    Task<T> CompleteAsync<T>(string prompt, CancellationToken ct = default) where T : class;
}
