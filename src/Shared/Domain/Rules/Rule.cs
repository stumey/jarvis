namespace Shared.Domain.Rules;

public sealed record Rule
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required RuleCondition Condition { get; init; }
    public required RuleAction Action { get; init; }
    public required bool IsActive { get; init; }
    public required DateTime CreatedAtUtc { get; init; }
}
