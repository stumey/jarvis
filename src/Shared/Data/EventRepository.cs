using System.Data;
using Dapper;
using Npgsql;

namespace Shared.Data;

public sealed class EventRepository : IEventRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public EventRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Guid> InsertAsync(EventRecord record, CancellationToken ct = default)
    {
        await using var conn = (NpgsqlConnection)_connectionFactory.CreateConnection();
        try
        {
            await conn.OpenAsync(ct);
            var cmd = new CommandDefinition(Sql.Events.Insert, record, cancellationToken: ct);
            return await conn.ExecuteScalarAsync<Guid>(cmd);
        }
        catch (NpgsqlException ex)
        {
            throw new DataException($"Failed to insert event: {ex.Message}", ex);
        }
    }

    public async Task<EventRecord?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var conn = (NpgsqlConnection)_connectionFactory.CreateConnection();
        try
        {
            await conn.OpenAsync(ct);
            var cmd = new CommandDefinition(Sql.Events.SelectById, new { Id = id }, cancellationToken: ct);
            return await conn.QuerySingleOrDefaultAsync<EventRecord>(cmd);
        }
        catch (NpgsqlException ex)
        {
            throw new DataException($"Failed to get event {id}: {ex.Message}", ex);
        }
    }

    public async Task<IReadOnlyList<EventRecord>> GetBySourceAsync(string source, int limit = 100, int offset = 0, CancellationToken ct = default)
    {
        await using var conn = (NpgsqlConnection)_connectionFactory.CreateConnection();
        try
        {
            await conn.OpenAsync(ct);
            var cmd = new CommandDefinition(Sql.Events.SelectBySource, new { Source = source, Limit = limit, Offset = offset }, cancellationToken: ct);
            var result = await conn.QueryAsync<EventRecord>(cmd);
            return result.AsList();
        }
        catch (NpgsqlException ex)
        {
            throw new DataException($"Failed to get events by source {source}: {ex.Message}", ex);
        }
    }

    public async Task<IReadOnlyList<EventRecord>> GetByTypeAsync(string eventType, int limit = 100, int offset = 0, CancellationToken ct = default)
    {
        await using var conn = (NpgsqlConnection)_connectionFactory.CreateConnection();
        try
        {
            await conn.OpenAsync(ct);
            var cmd = new CommandDefinition(Sql.Events.SelectByType, new { EventType = eventType, Limit = limit, Offset = offset }, cancellationToken: ct);
            var result = await conn.QueryAsync<EventRecord>(cmd);
            return result.AsList();
        }
        catch (NpgsqlException ex)
        {
            throw new DataException($"Failed to get events by type {eventType}: {ex.Message}", ex);
        }
    }

    public async Task<IReadOnlyList<EventRecord>> GetUnprocessedAsync(int limit = 100, CancellationToken ct = default)
    {
        await using var conn = (NpgsqlConnection)_connectionFactory.CreateConnection();
        try
        {
            await conn.OpenAsync(ct);
            var cmd = new CommandDefinition(Sql.Events.SelectUnprocessed, new { Limit = limit }, cancellationToken: ct);
            var result = await conn.QueryAsync<EventRecord>(cmd);
            return result.AsList();
        }
        catch (NpgsqlException ex)
        {
            throw new DataException($"Failed to get unprocessed events: {ex.Message}", ex);
        }
    }

    public async Task<IReadOnlyList<EventRecord>> GetRecentAsync(int limit = 100, int offset = 0, CancellationToken ct = default)
    {
        await using var conn = (NpgsqlConnection)_connectionFactory.CreateConnection();
        try
        {
            await conn.OpenAsync(ct);
            var cmd = new CommandDefinition(Sql.Events.SelectRecent, new { Limit = limit, Offset = offset }, cancellationToken: ct);
            var result = await conn.QueryAsync<EventRecord>(cmd);
            return result.AsList();
        }
        catch (NpgsqlException ex)
        {
            throw new DataException($"Failed to get recent events: {ex.Message}", ex);
        }
    }

    public async Task<IReadOnlyList<EventRecord>> SearchAsync(string query, int limit = 20, CancellationToken ct = default)
    {
        await using var conn = (NpgsqlConnection)_connectionFactory.CreateConnection();
        try
        {
            await conn.OpenAsync(ct);
            var cmd = new CommandDefinition(Sql.Events.Search, new { Query = query, Limit = limit }, cancellationToken: ct);
            var result = await conn.QueryAsync<EventRecord>(cmd);
            return result.AsList();
        }
        catch (NpgsqlException ex)
        {
            throw new DataException($"Failed to search events: {ex.Message}", ex);
        }
    }

    public async Task MarkProcessedAsync(Guid id, CancellationToken ct = default)
    {
        await using var conn = (NpgsqlConnection)_connectionFactory.CreateConnection();
        try
        {
            await conn.OpenAsync(ct);
            var cmd = new CommandDefinition(Sql.Events.MarkProcessed, new { Id = id }, cancellationToken: ct);
            await conn.ExecuteAsync(cmd);
        }
        catch (NpgsqlException ex)
        {
            throw new DataException($"Failed to mark event {id} as processed: {ex.Message}", ex);
        }
    }

    public async Task MarkProcessedBatchAsync(IReadOnlyList<Guid> ids, CancellationToken ct = default)
    {
        await using var conn = (NpgsqlConnection)_connectionFactory.CreateConnection();
        try
        {
            await conn.OpenAsync(ct);
            var cmd = new CommandDefinition(Sql.Events.MarkProcessedBatch, new { Ids = ids.ToArray() }, cancellationToken: ct);
            await conn.ExecuteAsync(cmd);
        }
        catch (NpgsqlException ex)
        {
            throw new DataException($"Failed to mark events as processed: {ex.Message}", ex);
        }
    }

    public async Task<long> CountAsync(CancellationToken ct = default)
    {
        await using var conn = (NpgsqlConnection)_connectionFactory.CreateConnection();
        try
        {
            await conn.OpenAsync(ct);
            var cmd = new CommandDefinition(Sql.Events.Count, cancellationToken: ct);
            return await conn.ExecuteScalarAsync<long>(cmd);
        }
        catch (NpgsqlException ex)
        {
            throw new DataException($"Failed to count events: {ex.Message}", ex);
        }
    }

    public async Task<long> CountUnprocessedAsync(CancellationToken ct = default)
    {
        await using var conn = (NpgsqlConnection)_connectionFactory.CreateConnection();
        try
        {
            await conn.OpenAsync(ct);
            var cmd = new CommandDefinition(Sql.Events.CountUnprocessed, cancellationToken: ct);
            return await conn.ExecuteScalarAsync<long>(cmd);
        }
        catch (NpgsqlException ex)
        {
            throw new DataException($"Failed to count unprocessed events: {ex.Message}", ex);
        }
    }
}
