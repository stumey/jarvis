namespace Shared.Contracts.Agent;

public sealed record AgentMessage(
    string Role,
    string Content,
    IReadOnlyList<AgentToolCall>? ToolCalls = null
);
