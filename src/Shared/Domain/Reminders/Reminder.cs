using System.Text.Json;

namespace Shared.Domain.Reminders;

public sealed record Reminder
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public required ReminderState State { get; init; }
    public DateTime? DueAtUtc { get; init; }
    public required DateTime CreatedAtUtc { get; init; }
    public required int Attempts { get; init; }
    public JsonDocument? Metadata { get; init; }
}
