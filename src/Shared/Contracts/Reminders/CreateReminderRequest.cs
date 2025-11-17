using System.Text.Json;

namespace Shared.Contracts.Reminders;

public sealed record CreateReminderRequest(
    string Title,
    DateTime? DueAtUtc = null,
    JsonDocument? Metadata = null
);
