using System.Text.Json;

namespace Shared.Contracts.Events;

public sealed record CreateEventRequest(
    Guid EventId,
    string Source,
    string EventType,
    DateTime TimestampUtc,
    string SchemaVersion,
    JsonDocument Payload,
    JsonDocument? Metadata = null
);
