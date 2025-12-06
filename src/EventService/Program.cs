using System.Data;
using System.Text.Json;
using Shared.Data;
using Shared.Validation;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("Postgres connection string is required");

builder.Services.AddDataServices(connectionString);
builder.Services.AddEventSchemaValidation();

var app = builder.Build();

app.MapPost("/events", async (
    HttpRequest request,
    IEventRepository repo,
    EventSchemaValidator validator,
    ILogger<Program> logger,
    CancellationToken ct) =>
{
    try
    {
        using var reader = new StreamReader(request.Body);
        var json = await reader.ReadToEndAsync(ct);

        var validationResult = validator.Validate(json);
        if (!validationResult.IsValid)
        {
            logger.LogWarning("Event validation failed: {Error}", validationResult.ErrorMessage);
            return Results.BadRequest(new { error = validationResult.ErrorMessage });
        }

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var record = new EventRecord
        {
            Id = root.GetProperty("event_id").GetGuid(),
            Source = root.GetProperty("source").GetString()!,
            EventType = root.GetProperty("event_type").GetString()!,
            TimestampUtc = root.GetProperty("timestamp").GetDateTime(),
            SchemaVersion = root.GetProperty("schema_version").GetString()!,
            Payload = root.GetProperty("payload").GetRawText(),
            Metadata = root.TryGetProperty("metadata", out var meta) ? meta.GetRawText() : null
        };

        var id = await repo.InsertAsync(record, ct);
        logger.LogInformation("Event {EventId} ingested from {Source}", id, record.Source);

        return Results.Created($"/events/{id}", new { id });
    }
    catch (JsonException ex)
    {
        logger.LogWarning(ex, "Invalid JSON in request body");
        return Results.BadRequest(new { error = "Invalid JSON format" });
    }
    catch (DataException ex)
    {
        logger.LogError(ex, "Database error while inserting event");
        return Results.Problem("Failed to store event", statusCode: 500);
    }
});

app.MapGet("/events/{id:guid}", async (
    Guid id,
    IEventRepository repo,
    ILogger<Program> logger,
    CancellationToken ct) =>
{
    try
    {
        var record = await repo.GetByIdAsync(id, ct);
        if (record is null)
            return Results.NotFound();

        return Results.Ok(record);
    }
    catch (DataException ex)
    {
        logger.LogError(ex, "Database error while fetching event {EventId}", id);
        return Results.Problem("Failed to fetch event", statusCode: 500);
    }
});

app.MapGet("/events", async (
    IEventRepository repo,
    ILogger<Program> logger,
    CancellationToken ct,
    string? source = null,
    string? type = null,
    bool? unprocessed = null,
    int limit = 100,
    int offset = 0) =>
{
    try
    {
        IReadOnlyList<EventRecord> records;

        if (unprocessed == true)
            records = await repo.GetUnprocessedAsync(limit, ct);
        else if (!string.IsNullOrEmpty(source))
            records = await repo.GetBySourceAsync(source, limit, offset, ct);
        else if (!string.IsNullOrEmpty(type))
            records = await repo.GetByTypeAsync(type, limit, offset, ct);
        else
            records = await repo.GetRecentAsync(limit, offset, ct);

        return Results.Ok(records);
    }
    catch (DataException ex)
    {
        logger.LogError(ex, "Database error while fetching events");
        return Results.Problem("Failed to fetch events", statusCode: 500);
    }
});

app.MapPost("/events/{id:guid}/processed", async (
    Guid id,
    IEventRepository repo,
    ILogger<Program> logger,
    CancellationToken ct) =>
{
    try
    {
        await repo.MarkProcessedAsync(id, ct);
        return Results.NoContent();
    }
    catch (DataException ex)
    {
        logger.LogError(ex, "Database error while marking event {EventId} as processed", id);
        return Results.Problem("Failed to update event", statusCode: 500);
    }
});

app.MapGet("/events/stats", async (
    IEventRepository repo,
    ILogger<Program> logger,
    CancellationToken ct) =>
{
    try
    {
        var total = await repo.CountAsync(ct);
        var unprocessed = await repo.CountUnprocessedAsync(ct);

        return Results.Ok(new { total, unprocessed, processed = total - unprocessed });
    }
    catch (DataException ex)
    {
        logger.LogError(ex, "Database error while fetching event stats");
        return Results.Problem("Failed to fetch stats", statusCode: 500);
    }
});

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();

public partial class Program { }
