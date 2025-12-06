using System.Data;
using Shared.Data;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("Postgres connection string is required");

builder.Services.AddDataServices(connectionString);

var app = builder.Build();

app.MapGet("/memory/search", async (
    string q,
    IEventRepository repo,
    ILogger<Program> logger,
    CancellationToken ct,
    int limit = 20) =>
{
    if (string.IsNullOrWhiteSpace(q))
        return Results.BadRequest(new { error = "Query parameter 'q' is required" });

    try
    {
        var results = await repo.SearchAsync(q, limit, ct);
        logger.LogInformation("Search for '{Query}' returned {Count} results", q, results.Count);
        return Results.Ok(results);
    }
    catch (DataException ex)
    {
        logger.LogError(ex, "Database error while searching");
        return Results.Problem("Failed to search", statusCode: 500);
    }
});

app.MapGet("/memory/recent", async (
    IEventRepository repo,
    ILogger<Program> logger,
    CancellationToken ct,
    int limit = 50,
    int offset = 0) =>
{
    try
    {
        var results = await repo.GetRecentAsync(limit, offset, ct);
        return Results.Ok(results);
    }
    catch (DataException ex)
    {
        logger.LogError(ex, "Database error while fetching recent events");
        return Results.Problem("Failed to fetch events", statusCode: 500);
    }
});

app.MapGet("/memory/by-source/{source}", async (
    string source,
    IEventRepository repo,
    ILogger<Program> logger,
    CancellationToken ct,
    int limit = 50,
    int offset = 0) =>
{
    try
    {
        var results = await repo.GetBySourceAsync(source, limit, offset, ct);
        return Results.Ok(results);
    }
    catch (DataException ex)
    {
        logger.LogError(ex, "Database error while fetching events by source");
        return Results.Problem("Failed to fetch events", statusCode: 500);
    }
});

app.MapGet("/memory/by-type/{eventType}", async (
    string eventType,
    IEventRepository repo,
    ILogger<Program> logger,
    CancellationToken ct,
    int limit = 50,
    int offset = 0) =>
{
    try
    {
        var results = await repo.GetByTypeAsync(eventType, limit, offset, ct);
        return Results.Ok(results);
    }
    catch (DataException ex)
    {
        logger.LogError(ex, "Database error while fetching events by type");
        return Results.Problem("Failed to fetch events", statusCode: 500);
    }
});

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();

public partial class Program { }
