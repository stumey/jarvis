using System.Text.Json;

namespace Shared.Domain.Rules;

public sealed record RuleCondition
{
    public required string Type { get; init; }
    public required JsonDocument Criteria { get; init; }
}
