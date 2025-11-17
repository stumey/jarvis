namespace Shared.Errors;

public class ValidationError : DomainError
{
    public ValidationError(string message) : base(message)
    {
    }

    public ValidationError(string message, Exception innerException) : base(message, innerException)
    {
    }
}
