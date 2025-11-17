namespace Shared.Abstractions;

public interface ISystemClock
{
    DateTime UtcNow { get; }
}
