using System.Text.Json;

namespace Shared.Contracts.Lists;

public sealed record AddShoppingItemRequest(
    Guid ListId,
    string ItemName,
    Guid? AddedByEvent = null,
    JsonDocument? Metadata = null
);
