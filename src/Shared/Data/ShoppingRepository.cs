using System.Data;
using Dapper;
using Npgsql;

namespace Shared.Data;

public sealed class ShoppingListRepository : IShoppingListRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ShoppingListRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Guid> CreateAsync(Guid userId, string name, CancellationToken ct = default)
    {
        await using var conn = (NpgsqlConnection)_connectionFactory.CreateConnection();
        try
        {
            await conn.OpenAsync(ct);
            var cmd = new CommandDefinition(Sql.ShoppingLists.Insert, new { Id = Guid.NewGuid(), UserId = userId, Name = name }, cancellationToken: ct);
            return await conn.ExecuteScalarAsync<Guid>(cmd);
        }
        catch (NpgsqlException ex)
        {
            throw new DataException($"Failed to create shopping list: {ex.Message}", ex);
        }
    }

    public async Task<ShoppingListRecord?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var conn = (NpgsqlConnection)_connectionFactory.CreateConnection();
        try
        {
            await conn.OpenAsync(ct);
            var cmd = new CommandDefinition(Sql.ShoppingLists.SelectById, new { Id = id }, cancellationToken: ct);
            return await conn.QuerySingleOrDefaultAsync<ShoppingListRecord>(cmd);
        }
        catch (NpgsqlException ex)
        {
            throw new DataException($"Failed to get shopping list: {ex.Message}", ex);
        }
    }

    public async Task<IReadOnlyList<ShoppingListRecord>> GetByUserAsync(Guid userId, CancellationToken ct = default)
    {
        await using var conn = (NpgsqlConnection)_connectionFactory.CreateConnection();
        try
        {
            await conn.OpenAsync(ct);
            var cmd = new CommandDefinition(Sql.ShoppingLists.SelectByUserId, new { UserId = userId }, cancellationToken: ct);
            var result = await conn.QueryAsync<ShoppingListRecord>(cmd);
            return result.AsList();
        }
        catch (NpgsqlException ex)
        {
            throw new DataException($"Failed to get shopping lists: {ex.Message}", ex);
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await using var conn = (NpgsqlConnection)_connectionFactory.CreateConnection();
        try
        {
            await conn.OpenAsync(ct);
            var cmd = new CommandDefinition(Sql.ShoppingLists.Delete, new { Id = id }, cancellationToken: ct);
            await conn.ExecuteAsync(cmd);
        }
        catch (NpgsqlException ex)
        {
            throw new DataException($"Failed to delete shopping list: {ex.Message}", ex);
        }
    }
}

public sealed class ShoppingItemRepository : IShoppingItemRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ShoppingItemRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Guid> AddAsync(Guid listId, string name, string? quantity = null, string? notes = null, CancellationToken ct = default)
    {
        await using var conn = (NpgsqlConnection)_connectionFactory.CreateConnection();
        try
        {
            await conn.OpenAsync(ct);
            var cmd = new CommandDefinition(Sql.ShoppingItems.Insert,
                new { Id = Guid.NewGuid(), ListId = listId, Name = name, Quantity = quantity, Status = "needed" },
                cancellationToken: ct);
            return await conn.ExecuteScalarAsync<Guid>(cmd);
        }
        catch (NpgsqlException ex)
        {
            throw new DataException($"Failed to add shopping item: {ex.Message}", ex);
        }
    }

    public async Task<IReadOnlyList<ShoppingItemRecord>> GetByListAsync(Guid listId, CancellationToken ct = default)
    {
        await using var conn = (NpgsqlConnection)_connectionFactory.CreateConnection();
        try
        {
            await conn.OpenAsync(ct);
            var cmd = new CommandDefinition(Sql.ShoppingItems.SelectByListId, new { ListId = listId }, cancellationToken: ct);
            var result = await conn.QueryAsync<ShoppingItemRecord>(cmd);
            return result.AsList();
        }
        catch (NpgsqlException ex)
        {
            throw new DataException($"Failed to get shopping items: {ex.Message}", ex);
        }
    }

    public async Task<IReadOnlyList<ShoppingItemRecord>> GetByStatusAsync(Guid listId, string status, CancellationToken ct = default)
    {
        await using var conn = (NpgsqlConnection)_connectionFactory.CreateConnection();
        try
        {
            await conn.OpenAsync(ct);
            const string sql = """
                SELECT id, list_id AS ListId, name, status, quantity, notes, metadata, created_at AS CreatedAt, updated_at AS UpdatedAt
                FROM shopping_items WHERE list_id = @ListId AND status = @Status ORDER BY created_at ASC
                """;
            var cmd = new CommandDefinition(sql, new { ListId = listId, Status = status }, cancellationToken: ct);
            var result = await conn.QueryAsync<ShoppingItemRecord>(cmd);
            return result.AsList();
        }
        catch (NpgsqlException ex)
        {
            throw new DataException($"Failed to get shopping items: {ex.Message}", ex);
        }
    }

    public async Task UpdateStatusAsync(Guid id, string status, CancellationToken ct = default)
    {
        await using var conn = (NpgsqlConnection)_connectionFactory.CreateConnection();
        try
        {
            await conn.OpenAsync(ct);
            var cmd = new CommandDefinition(Sql.ShoppingItems.UpdateStatus, new { Id = id, Status = status }, cancellationToken: ct);
            await conn.ExecuteAsync(cmd);
        }
        catch (NpgsqlException ex)
        {
            throw new DataException($"Failed to update shopping item: {ex.Message}", ex);
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await using var conn = (NpgsqlConnection)_connectionFactory.CreateConnection();
        try
        {
            await conn.OpenAsync(ct);
            var cmd = new CommandDefinition(Sql.ShoppingItems.Delete, new { Id = id }, cancellationToken: ct);
            await conn.ExecuteAsync(cmd);
        }
        catch (NpgsqlException ex)
        {
            throw new DataException($"Failed to delete shopping item: {ex.Message}", ex);
        }
    }
}
