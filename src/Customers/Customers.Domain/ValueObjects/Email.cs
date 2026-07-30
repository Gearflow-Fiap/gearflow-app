using Shared.Domain.Primitives;

namespace Customers.Domain.ValueObjects;

public sealed class Email : ValueObject
{
    public string Value { get; }

    private Email(string value) => Value = value;

    public static Result<Email> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || !value.Contains('@'))
            return Result.Failure<Email>(Error.Validation("Email.Invalid", "E-mail inválido.", "email"));

        return Result.Success(new Email(value.Trim()));
    }

    public static Email FromTrusted(string value) => new(value);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
