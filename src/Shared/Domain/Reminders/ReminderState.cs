namespace Shared.Domain.Reminders;

public enum ReminderState
{
    Scheduled,
    Sending,
    Sent,
    Acknowledged,
    Ignored,
    Failed
}
