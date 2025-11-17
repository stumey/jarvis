namespace Shared.Errors;

public class DomainError : Exception
{
    public DomainError(string message) : base(message)
    {
    }

    public DomainError(string message, Exception innerException) : base(message, innerException)
    {
    }
}
