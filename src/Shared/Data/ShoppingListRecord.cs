namespace Shared.Data;

public sealed class ShoppingListRecord
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class ShoppingItemRecord
{
    public Guid Id { get; set; }
    public Guid ListId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "needed";
    public Guid? AddedByEventId { get; set; }
    public string? DetectedFrom { get; set; }
    public string? Quantity { get; set; }
    public string? Notes { get; set; }
    public string? Metadata { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
