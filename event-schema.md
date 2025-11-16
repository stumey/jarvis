Event Schema v1 — Validation Plan (Markdown Spec)
Overview
This document defines:
event_schema_v1.json — canonical schema for all ingested events
C# validator using
System.Text.Json for parsing
JsonSchema.Net for validation
xUnit tests validating the schema + validator functionality
Integration guidance for EventService
This file is safe to provide directly to Claude Code.
1. event_schema_v1.json
Save to:
/src/Shared/Schemas/event_schema_v1.json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "$id": "https://yourdomain.com/schemas/event_schema_v1.json",
  "title": "Event Schema v1",
  "description": "Unified event schema for ingestion from connectors and services",
  "type": "object",
  "required": [
    "event_id",
    "source",
    "event_type",
    "timestamp",
    "schema_version",
    "payload"
  ],
  "properties": {
    "event_id": {
      "type": "string",
      "format": "uuid",
      "description": "Unique identifier for the event"
    },
    "source": {
      "type": "string",
      "enum": [
        "email",
        "calendar",
        "bank",
        "manual",
        "voice",
        "task",
        "shopping_list",
        "system"
      ]
    },
    "event_type": {
      "type": "string",
      "enum": [
        "email_received",
        "bill_statement",
        "payment_due",
        "calendar_event_created",
        "calendar_event_updated",
        "transaction_posted",
        "task_created",
        "task_completed",
        "shopping_item_added",
        "shopping_item_removed",
        "manual_note",
        "system_generated"
      ]
    },
    "timestamp": {
      "type": "string",
      "format": "date-time"
    },
    "schema_version": {
      "type": "string",
      "enum": ["1"]
    },
    "payload": {
      "type": "object",
      "additionalProperties": true
    },
    "metadata": {
      "type": "object",
      "additionalProperties": true
    }
  },
  "additionalProperties": false
}
2. C# Validator Implementation
Save to:
/src/EventService/Validation/EventSchemaValidator.cs
Uses both:
System.Text.Json — parsing JSON into JsonDocument
JsonSchema.Net — strict schema evaluation
NuGet packages:
dotnet add package JsonSchema.Net
dotnet add package JsonSchema.Net.Data
using Json.Schema;
using System.Text.Json;

namespace EventService.Validation;

public class EventSchemaValidator
{
    private readonly JsonSchema _schema;

    public EventSchemaValidator()
    {
        var schemaJson = File.ReadAllText("Schemas/event_schema_v1.json");
        _schema = JsonSerializer.Deserialize<JsonSchema>(schemaJson)!;
    }

    public EventSchemaValidationResult Validate(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var result = _schema.Evaluate(doc.RootElement, new EvaluationOptions
            {
                OutputFormat = OutputFormat.Detailed
            });

            return result.IsValid
                ? EventSchemaValidationResult.Success()
                : EventSchemaValidationResult.Failure(result.Output.ToJsonString());
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
3. Example Integration in EventService Endpoint
app.MapPost("/events", async (
    HttpContext context,
    EventSchemaValidator validator,
    EventRepository repo) =>
{
    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync();

    var validation = validator.Validate(body);
    if (!validation.IsValid)
        return Results.BadRequest(new { error = validation.ErrorMessage });

    var evt = JsonSerializer.Deserialize<EventRecord>(body)!;

    await repo.InsertEventAsync(evt);

    return Results.Created($"/events/{evt.event_id}", evt);
});
4. xUnit Tests
Save to:
/tests/EventService.Tests/EventSchemaValidatorTests.cs
NuGet dependencies:
dotnet add package xunit
dotnet add package xunit.runner.visualstudio
dotnet add package FluentAssertions
4.1 Valid Event
[Fact]
public void ValidEvent_ShouldPass()
{
    var validator = new EventSchemaValidator();

    var json = """
    {
      "event_id": "3e0ab897-2667-42c8-88e6-b3eb5fe2715f",
      "source": "email",
      "event_type": "email_received",
      "timestamp": "2025-11-16T17:30:00Z",
      "schema_version": "1",
      "payload": {
          "from": "billing@utility.com",
          "subject": "Your monthly bill"
      }
    }
    """;

    var result = validator.Validate(json);

    result.IsValid.Should().BeTrue();
}
4.2 Missing Required Field
[Fact]
public void MissingRequiredField_ShouldFail()
{
    var validator = new EventSchemaValidator();

    var json = """
    {
      "source": "email",
      "event_type": "email_received",
      "timestamp": "2025-11-16T17:30:00Z",
      "schema_version": "1",
      "payload": {}
    }
    """;

    var result = validator.Validate(json);

    result.IsValid.Should().BeFalse();
    result.ErrorMessage.Should().Contain("event_id");
}
4.3 Invalid Enum
[Fact]
public void InvalidEnum_ShouldFail()
{
    var validator = new EventSchemaValidator();

    var json = """
    {
      "event_id": "3e0ab897-2667-42c8-88e6-b3eb5fe2715f",
      "source": "gmail",
      "event_type": "email_received",
      "timestamp": "2025-11-16T17:30:00Z",
      "schema_version": "1",
      "payload": {}
    }
    """;

    var result = validator.Validate(json);

    result.IsValid.Should().BeFalse();
    result.ErrorMessage.Should().Contain("source");
}
4.4 Additional Properties Not Allowed
[Fact]
public void AdditionalProperty_ShouldFail()
{
    var validator = new EventSchemaValidator();

    var json = """
    {
      "event_id": "3e0ab897-2667-42c8-88e6-b3eb5fe2715f",
      "source": "email",
      "event_type": "email_received",
      "timestamp": "2025-11-16T17:30:00Z",
      "schema_version": "1",
      "payload": {},
      "unexpected": "extra"
    }
    """;

    var result = validator.Validate(json);

    result.IsValid.Should().BeFalse();
    result.ErrorMessage.Should().Contain("additionalProperties");
}
5. Recommended Dapper DTO
To ensure schema ↔ DB alignment:
public record EventRecord(
    Guid event_id,
    string source,
    string event_type,
    DateTime timestamp,
    string schema_version,
    JsonDocument payload,
    JsonDocument? metadata = null
);
Works perfectly with Dapper + System.Text.Json.