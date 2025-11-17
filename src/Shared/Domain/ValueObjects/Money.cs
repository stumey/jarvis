using Shared.Errors;

namespace Shared.Domain.ValueObjects;

public readonly record struct Money
{
    public decimal Amount { get; init; }
    public string Currency { get; init; }

    public Money(decimal amount, string currency)
    {
        if (amount < 0)
            throw new ValidationError("Money amount cannot be negative.");

        if (string.IsNullOrWhiteSpace(currency))
            throw new ValidationError("Currency is required.");

        Amount = amount;
        Currency = currency;
    }

    public override string ToString() => $"{Amount:F2} {Currency}";
}
