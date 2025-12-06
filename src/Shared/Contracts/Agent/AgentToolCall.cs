using System.Text.Json;

namespace Shared.Contracts.Agent;

public sealed record AgentToolCall(
    string Id,
    string Name,
    JsonDocument Arguments
);
