using System.Text.Json;

namespace Shared.Domain.Users;

public sealed record UserPreferences
{
    public required Guid UserId { get; init; }
    public required JsonDocument Preferences { get; init; }
    public required DateTime UpdatedAtUtc { get; init; }
}
