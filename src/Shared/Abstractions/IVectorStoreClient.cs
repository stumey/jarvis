using System.Text.Json;

namespace Shared.Abstractions;

public interface IVectorStoreClient
{
    Task UpsertAsync(string collection, string id, float[] vector, JsonDocument? payload = null, CancellationToken ct = default);
    Task<IReadOnlyList<VectorSearchResult>> SearchAsync(string collection, float[] vector, int limit = 10, CancellationToken ct = default);
    Task DeleteAsync(string collection, string id, CancellationToken ct = default);
}

public sealed record VectorSearchResult(
    string Id,
    float Score,
    JsonDocument? Payload
);
