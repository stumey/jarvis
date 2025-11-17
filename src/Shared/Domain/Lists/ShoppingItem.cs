using System.Text.Json;

namespace Shared.Domain.Lists;

public sealed record ShoppingItem
{
    public required Guid ItemId { get; init; }
    public required Guid ListId { get; init; }
    public required string Name { get; init; }
    public required ShoppingItemStatus Status { get; init; }
    public required DateTime AddedAtUtc { get; init; }
    public Guid? AddedByEvent { get; init; }
    public JsonDocument? Metadata { get; init; }
}
