using System.Text.Json;

namespace Shared.Domain.Events;

public sealed record Event
{
    public required Guid Id { get; init; }
    public required EventSource Source { get; init; }
    public required EventType Type { get; init; }
    public required DateTime TimestampUtc { get; init; }
    public required string SchemaVersion { get; init; } = "1";
    public required JsonDocument Payload { get; init; }
    public JsonDocument? Metadata { get; init; }
}
