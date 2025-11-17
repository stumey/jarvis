using System.Text.Json;

namespace Shared.Domain.Patterns;

public sealed record Pattern
{
    public required Guid Id { get; init; }
    public required Guid UserId { get; init; }
    public required string Type { get; init; }
    public required string Description { get; init; }
    public required double Confidence { get; init; }
    public required string Frequency { get; init; }
    public DateTime? NextOccurrence { get; init; }
    public required DateTime DetectedAtUtc { get; init; }
    public JsonDocument? Metadata { get; init; }
}
