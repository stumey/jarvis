namespace Shared.Data;

public sealed class EventRecord
{
    public Guid Id { get; set; }
    public string Source { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public DateTime TimestampUtc { get; set; }
    public string SchemaVersion { get; set; } = "1";
    public string Payload { get; set; } = "{}";
    public string? Metadata { get; set; }
    public bool Processed { get; set; }
    public DateTime CreatedAt { get; set; }
}
