namespace AgentService.Tools;

public sealed class ToolExecutor
{
    private readonly Dictionary<string, ITool> _tools = new();

    public void RegisterTool(ITool tool)
    {
        _tools[tool.Name] = tool;
    }

    public List<ToolDefinition> GetToolDefinitions()
    {
        return _tools.Values
            .Select(t => new ToolDefinition(t.Name, t.Description, t.InputSchema))
            .ToList();
    }

    public async Task<string> ExecuteToolAsync(string toolName, Dictionary<string, object> input, CancellationToken ct = default)
    {
        if (!_tools.TryGetValue(toolName, out var tool))
            throw new InvalidOperationException($"Tool '{toolName}' not found");

        return await tool.ExecuteAsync(input, ct);
    }

    public bool HasTool(string toolName) => _tools.ContainsKey(toolName);
}
