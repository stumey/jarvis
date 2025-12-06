namespace Shared.Data;

public interface IShoppingListRepository
{
    Task<Guid> CreateAsync(Guid userId, string name, CancellationToken ct = default);
    Task<ShoppingListRecord?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<ShoppingListRecord>> GetByUserAsync(Guid userId, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

public interface IShoppingItemRepository
{
    Task<Guid> AddAsync(Guid listId, string name, string? quantity = null, string? notes = null, CancellationToken ct = default);
    Task<IReadOnlyList<ShoppingItemRecord>> GetByListAsync(Guid listId, CancellationToken ct = default);
    Task<IReadOnlyList<ShoppingItemRecord>> GetByStatusAsync(Guid listId, string status, CancellationToken ct = default);
    Task UpdateStatusAsync(Guid id, string status, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
