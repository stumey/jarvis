namespace Shared.Domain.Events;

public enum EventType
{
    EmailReceived,
    BillStatement,
    PaymentDue,
    CalendarEventCreated,
    CalendarEventUpdated,
    TransactionPosted,
    TaskCreated,
    TaskCompleted,
    ShoppingItemAdded,
    ShoppingItemRemoved,
    ManualNote,
    SystemGenerated
}
