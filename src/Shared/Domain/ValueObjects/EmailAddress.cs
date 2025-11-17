using Shared.Errors;
using System.Text.RegularExpressions;

namespace Shared.Domain.ValueObjects;

public readonly record struct EmailAddress
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string Value { get; init; }

    public EmailAddress(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ValidationError("Email address cannot be empty.");

        if (!EmailRegex.IsMatch(value))
            throw new ValidationError($"Invalid email address: {value}");

        Value = value;
    }

    public override string ToString() => Value;

    public static implicit operator string(EmailAddress email) => email.Value;
}
