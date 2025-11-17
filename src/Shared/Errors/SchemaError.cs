namespace Shared.Errors;

public class SchemaError : DomainError
{
    public SchemaError(string message) : base(message)
    {
    }

    public SchemaError(string message, Exception innerException) : base(message, innerException)
    {
    }
}
