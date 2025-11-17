namespace Shared.Domain.ValueObjects;

public readonly record struct IsoDateTime(DateTime Value)
{
    public override string ToString() => Value.ToUniversalTime().ToString("o");

    public static implicit operator DateTime(IsoDateTime isoDateTime) => isoDateTime.Value;
    public static implicit operator IsoDateTime(DateTime dateTime) => new(dateTime);
}
