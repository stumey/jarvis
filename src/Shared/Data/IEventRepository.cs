namespace Shared.Data;

public interface IEventRepository
{
    Task<Guid> InsertAsync(EventRecord record, CancellationToken ct = default);
    Task<EventRecord?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<EventRecord>> GetBySourceAsync(string source, int limit = 100, int offset = 0, CancellationToken ct = default);
    Task<IReadOnlyList<EventRecord>> GetByTypeAsync(string eventType, int limit = 100, int offset = 0, CancellationToken ct = default);
    Task<IReadOnlyList<EventRecord>> GetUnprocessedAsync(int limit = 100, CancellationToken ct = default);
    Task<IReadOnlyList<EventRecord>> GetRecentAsync(int limit = 100, int offset = 0, CancellationToken ct = default);
    Task<IReadOnlyList<EventRecord>> SearchAsync(string query, int limit = 20, CancellationToken ct = default);
    Task MarkProcessedAsync(Guid id, CancellationToken ct = default);
    Task MarkProcessedBatchAsync(IReadOnlyList<Guid> ids, CancellationToken ct = default);
    Task<long> CountAsync(CancellationToken ct = default);
    Task<long> CountUnprocessedAsync(CancellationToken ct = default);
}
