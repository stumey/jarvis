using System.Text.Json;

namespace Shared.Contracts.Agent;

public sealed record AgentToolResult(
    string ToolCallId,
    bool Success,
    JsonDocument? Result = null,
    string? Error = null
);
