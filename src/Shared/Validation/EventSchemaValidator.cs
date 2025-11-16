using Json.Schema;
using System.Text.Json;

namespace Shared.Validation;

/// <summary>
/// Validates events against the canonical event schema v1.
/// Register as a singleton in DI for optimal performance.
/// </summary>
public class EventSchemaValidator
{
    private readonly JsonSchema _schema;

    /// <summary>
    /// Creates a validator with a pre-loaded schema.
    /// </summary>
    public EventSchemaValidator(JsonSchema schema)
    {
        _schema = schema ?? throw new ArgumentNullException(nameof(schema));
    }

    public EventSchemaValidationResult Validate(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var result = _schema.Evaluate(doc.RootElement);

            return result.IsValid
                ? EventSchemaValidationResult.Success()
                : EventSchemaValidationResult.Failure("Schema validation failed");
        }
        catch (JsonException ex)
        {
            return EventSchemaValidationResult.Failure($"Invalid JSON: {ex.Message}");
        }
    }
}

public record EventSchemaValidationResult(bool IsValid, string? ErrorMessage = null)
{
    public static EventSchemaValidationResult Success() =>
        new(true, null);

    public static EventSchemaValidationResult Failure(string message) =>
        new(false, message);
}
