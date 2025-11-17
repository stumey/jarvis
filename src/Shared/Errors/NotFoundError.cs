namespace Shared.Errors;

public class NotFoundError : DomainError
{
    public NotFoundError(string message) : base(message)
    {
    }

    public NotFoundError(string resourceType, string resourceId)
        : base($"{resourceType} with ID '{resourceId}' was not found.")
    {
    }
}
