namespace Shared.Contracts.Lists;

public sealed record ShoppingListResponse(
    Guid ListId,
    string Name,
    DateTime CreatedAtUtc,
    IReadOnlyList<ShoppingItemResponse> Items
);

public sealed record ShoppingItemResponse(
    Guid ItemId,
    string Name,
    string Status,
    DateTime AddedAtUtc
);
