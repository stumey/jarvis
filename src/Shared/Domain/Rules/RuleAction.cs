using System.Text.Json;

namespace Shared.Domain.Rules;

public sealed record RuleAction
{
    public required string Type { get; init; }
    public required JsonDocument Parameters { get; init; }
}
