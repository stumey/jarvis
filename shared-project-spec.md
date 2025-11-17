Shared Project — Specification Document (v2, SOLID/DDD-Aligned)
Purpose: Provide pure domain primitives, contracts, and cross-service abstractions that support all microservices in the AI Assistant system.
This project must be:
Pure (no HTTP, no data access, no DI inside Shared)
Stable (domain should change rarely)
SOLID (especially SRP, DIP)
Testable
Minimal
Fast (no heavy serialization layers)
Modern (.NET 8 idioms, file-scoped namespaces, records, readonly structs, required properties, Span-friendly types where reasonable)
1. High-Level Architectural Goals
The Shared project must:
✔ Separate Domain from Contracts from Infrastructure Abstractions
Following DDD-lite approach:
Domain = invariants, core models, value objects
Contracts = DTOs for API/agent/tool calling
Abstractions = clean interfaces for external interactions (Claude client, vector store, etc.)
Errors = domain errors & validation errors
Utilities = JSON configuration, parsing helpers
Schemas = raw JSON schemas (no schema loader logic here)
✔ No business logic in Shared
Avoid:
DB access
Claude access
API calls
Timezone logic
Complex parsing
✔ The Shared project must be referenced by every microservice, but must not take dependencies on them.
✔ Domain models must be Dapper-friendly, immutable, and match DB tables precisely.
✔ Domain models must use System.Text.Json only.
2. Project Structure (Improved)
/Shared
    /Domain
        Events/
            Event.cs
            EventSource.cs (enum)
            EventType.cs (enum)
        Reminders/
            Reminder.cs
            ReminderState.cs (enum)
        Lists/
            ShoppingItem.cs
            ShoppingItemStatus.cs (enum)
        Patterns/
            Pattern.cs
        Rules/
            Rule.cs
            RuleCondition.cs
            RuleAction.cs
        Users/
            UserPreferences.cs

        /ValueObjects
            EventId.cs
            ReminderId.cs
            ListId.cs
            Money.cs
            EmailAddress.cs
            IsoDateTime.cs
            NaturalLanguageText.cs

    /Contracts
        Events/
            CreateEventRequest.cs
            EventResponse.cs

        Reminders/
            CreateReminderRequest.cs

        Lists/
            AddShoppingItemRequest.cs
            ShoppingListResponse.cs

        Patterns/
            PatternDetectionBatch.cs
            PatternResult.cs

        Rules/
            EvaluateRulesRequest.cs

        Agent/
            AgentMessage.cs
            AgentToolCall.cs
            AgentToolResult.cs

    /Abstractions
        IClaudeModelClient.cs
        IVectorStoreClient.cs
        ITemporalParser.cs
        ISystemClock.cs
        ICorrelationIdProvider.cs

    /Errors
        DomainError.cs
        ValidationError.cs
        NotFoundError.cs
        SchemaError.cs

    /Serialization
        JsonOptions.cs
        JsonNamingPolicies.cs

    /Utils
        EnumUtils.cs
        DomainValidation.cs

    /Schemas
        event_schema_v1.json

    Shared.csproj
3. Domain Layer (Clean, Immutable, DDD-Lite)
Domain models must:
Be immutable (record or readonly struct where appropriate)
Enforce invariants in constructors
Avoid serialization attributes except JsonPropertyName
Use required where appropriate (C# 11 feature)
Not include behavior that belongs in services
3.1 Event.cs
namespace Shared.Domain.Events;

public sealed record Event
{
    public required EventId Id { get; init; }
    public required EventSource Source { get; init; }
    public required EventType Type { get; init; }
    public required DateTime TimestampUtc { get; init; }
    public required string SchemaVersion { get; init; } = "1";
    public required JsonDocument Payload { get; init; }
    public JsonDocument? Metadata { get; init; }
}
3.2 Reminder.cs (DDD-compliant but passive)
namespace Shared.Domain.Reminders;

public sealed record Reminder
{
    public required ReminderId Id { get; init; }
    public required string Title { get; init; }
    public required ReminderState State { get; init; }
    public DateTime? DueAtUtc { get; init; }
    public required DateTime CreatedAtUtc { get; init; }
    public required int Attempts { get; init; }
    public JsonDocument? Metadata { get; init; }
}
3.3 ShoppingItem.cs
namespace Shared.Domain.Lists;

public sealed record ShoppingItem
{
    public required Guid ItemId { get; init; }
    public required ListId ListId { get; init; }
    public required string Name { get; init; }
    public required ShoppingItemStatus Status { get; init; }
    public required DateTime AddedAtUtc { get; init; }
    public EventId? AddedByEvent { get; init; }
    public JsonDocument? Metadata { get; init; }
}
3.4 Patterns, Rules, Preferences (Simple Records)
These remain similar but now use strict invariants in constructors.
4. Value Objects (Pure, Safe, Small)
Design goals:
Represent true domain concepts
Be immutable
Validate in constructor
Provide helpful factory methods
Support implicit conversions
EventId.cs
public readonly record struct EventId(Guid Value)
{
    public EventId
    {
        if (Value == Guid.Empty)
            throw new ValidationError("EventId cannot be empty.");
    }

    public static EventId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}
Money.cs (currency-aware)
public readonly record struct Money(decimal Amount, string Currency)
{
    public Money
    {
        if (Amount < 0) throw new ValidationError("Money cannot be negative.");
        if (string.IsNullOrWhiteSpace(Currency)) throw new ValidationError("Currency required.");
    }

    public override string ToString() => $"{Amount} {Currency}";
}
IsoDateTime.cs
public readonly record struct IsoDateTime(DateTime Value)
{
    public override string ToString() => Value.ToUniversalTime().ToString("o");
}
5. Contracts Layer (API DTOs)
Contracts follow:
No domain invariants
Flat simple shapes
System.Text.Json serialization
No logic
No validation
This keeps the DTO layer stable and easily versioned.
Example:
public sealed record CreateEventRequest(
    Guid EventId,
    string Source,
    string EventType,
    DateTime TimestampUtc,
    string SchemaVersion,
    JsonDocument Payload,
    JsonDocument? Metadata = null
);
6. Abstractions (DIP-Compliant Interfaces)
These interfaces belong in Shared to avoid leaking infrastructure concerns into domain logic.
6.1 IClaudeModelClient
public interface IClaudeModelClient
{
    Task<string> CompleteAsync(object request, CancellationToken ct = default);
}
6.2 IVectorStoreClient
public interface IVectorStoreClient
{
    Task UpsertEmbeddingAsync(string collection, string id, float[] vector, JsonDocument payload);
    Task<IReadOnlyList<VectorSearchResult>> SearchAsync(string collection, float[] vector, int limit);
}
6.3 ISystemClock (essential for testability)
public interface ISystemClock
{
    DateTime UtcNow { get; }
}
(Each microservice provides implementation.)
7. Errors Layer
public class DomainError : Exception { ... }
public class ValidationError : Exception { ... }
public class SchemaError : Exception { ... }
public class NotFoundError : Exception { ... }
These support clean domain + service-level error handling.
8. Serialization Utilities
One place to define:
Naming policy
Serializer options
Converters
public static class JsonOptions
{
    public static readonly JsonSerializerOptions Default = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        AllowTrailingCommas = true,
        WriteIndented = false
    };
}
9. Schemas
The Shared project stores schemas only.
Validation logic lives in EventService, keeping Shared pure.
/Schemas/event_schema_v1.json
10. Testing Requirements
Shared project tests must verify:
Value object validation
Serialization correctness
Enum mappings
DTO struct correctness
Domain records remain immutable
Required property usage
Unit tests must run with:
net8.0
No external dependencies
11. Deliverables Required From Claude Code
Claude must generate:
The full folder structure
All domain models
Value objects with invariant enforcement
All DTO contracts
All abstractions & interfaces
All error types
Serialization configuration
Utility classes
Tests for domain + contracts
Clean csproj using .NET 8
Summary of Improvements vs Previous Iteration
✔ Separation of concerns (SOLID)
✔ True DDD-lite boundaries
✔ Pure domain-focused Shared project
✔ No infrastructure creeping in
✔ Improved maintainability
✔ Clear abstractions
✔ More powerful & realistic DTOs
✔ Better value objects
✔ Better enum + validation strategy
✔ Testable, stable, idiomatic .NET 8