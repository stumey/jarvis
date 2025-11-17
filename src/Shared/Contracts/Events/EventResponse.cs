using System.Text.Json;

namespace Shared.Contracts.Events;

public sealed record EventResponse(
    Guid Id,
    string Source,
    string EventType,
    DateTime TimestampUtc,
    string SchemaVersion,
    JsonDocument Payload,
    JsonDocument? Metadata
);
