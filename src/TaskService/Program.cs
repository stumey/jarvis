using System.Data;
using Shared.Data;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("Postgres connection string is required");

builder.Services.AddDataServices(connectionString);

var app = builder.Build();

app.MapPost("/lists", async (
    Guid userId,
    string name,
    IShoppingListRepository repo,
    ILogger<Program> logger,
    CancellationToken ct) =>
{
    try
    {
        var id = await repo.CreateAsync(userId, name, ct);
        return Results.Created($"/lists/{id}", new { id });
    }
    catch (DataException ex)
    {
        logger.LogError(ex, "Database error while creating list");
        return Results.Problem("Failed to create list", statusCode: 500);
    }
});

app.MapGet("/lists/user/{userId:guid}", async (
    Guid userId,
    IShoppingListRepository repo,
    ILogger<Program> logger,
    CancellationToken ct) =>
{
    try
    {
        var lists = await repo.GetByUserAsync(userId, ct);
        return Results.Ok(lists);
    }
    catch (DataException ex)
    {
        logger.LogError(ex, "Database error while fetching lists");
        return Results.Problem("Failed to fetch lists", statusCode: 500);
    }
});

app.MapGet("/lists/{id:guid}", async (
    Guid id,
    IShoppingListRepository repo,
    ILogger<Program> logger,
    CancellationToken ct) =>
{
    try
    {
        var list = await repo.GetByIdAsync(id, ct);
        if (list is null)
            return Results.NotFound();
        return Results.Ok(list);
    }
    catch (DataException ex)
    {
        logger.LogError(ex, "Database error while fetching list");
        return Results.Problem("Failed to fetch list", statusCode: 500);
    }
});

app.MapDelete("/lists/{id:guid}", async (
    Guid id,
    IShoppingListRepository repo,
    ILogger<Program> logger,
    CancellationToken ct) =>
{
    try
    {
        await repo.DeleteAsync(id, ct);
        return Results.NoContent();
    }
    catch (DataException ex)
    {
        logger.LogError(ex, "Database error while deleting list");
        return Results.Problem("Failed to delete list", statusCode: 500);
    }
});

app.MapPost("/lists/{listId:guid}/items", async (
    Guid listId,
    string name,
    IShoppingItemRepository repo,
    ILogger<Program> logger,
    CancellationToken ct,
    string? quantity = null,
    string? notes = null) =>
{
    try
    {
        var id = await repo.AddAsync(listId, name, quantity, notes, ct);
        return Results.Created($"/items/{id}", new { id });
    }
    catch (DataException ex)
    {
        logger.LogError(ex, "Database error while adding item");
        return Results.Problem("Failed to add item", statusCode: 500);
    }
});

app.MapGet("/lists/{listId:guid}/items", async (
    Guid listId,
    IShoppingItemRepository repo,
    ILogger<Program> logger,
    CancellationToken ct,
    string? status = null) =>
{
    try
    {
        var items = status != null
            ? await repo.GetByStatusAsync(listId, status, ct)
            : await repo.GetByListAsync(listId, ct);
        return Results.Ok(items);
    }
    catch (DataException ex)
    {
        logger.LogError(ex, "Database error while fetching items");
        return Results.Problem("Failed to fetch items", statusCode: 500);
    }
});

app.MapPatch("/items/{id:guid}/status", async (
    Guid id,
    string status,
    IShoppingItemRepository repo,
    ILogger<Program> logger,
    CancellationToken ct) =>
{
    try
    {
        await repo.UpdateStatusAsync(id, status, ct);
        return Results.NoContent();
    }
    catch (DataException ex)
    {
        logger.LogError(ex, "Database error while updating item status");
        return Results.Problem("Failed to update item", statusCode: 500);
    }
});

app.MapDelete("/items/{id:guid}", async (
    Guid id,
    IShoppingItemRepository repo,
    ILogger<Program> logger,
    CancellationToken ct) =>
{
    try
    {
        await repo.DeleteAsync(id, ct);
        return Results.NoContent();
    }
    catch (DataException ex)
    {
        logger.LogError(ex, "Database error while deleting item");
        return Results.Problem("Failed to delete item", statusCode: 500);
    }
});

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();

public partial class Program { }
