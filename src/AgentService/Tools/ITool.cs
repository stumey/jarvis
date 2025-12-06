namespace AgentService.Tools;

public interface ITool
{
    string Name { get; }
    string Description { get; }
    object InputSchema { get; }
    Task<string> ExecuteAsync(Dictionary<string, object> input, CancellationToken ct = default);
}
