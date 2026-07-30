using Customers.Domain.Common;
using Shared.Domain.Primitives;

namespace Customers.Domain.ValueObjects;

/// <summary>CNPJ do cliente (só dígitos). Regra de validação preservada do GearFlow.</summary>
public sealed class Cnpj : ValueObject
{
    public string Value { get; }

    private Cnpj(string value) => Value = value;

    public static Result<Cnpj> Create(string value)
    {
        if (!DocumentValidation.IsValidCnpj(value))
            return Result.Failure<Cnpj>(Error.Validation("Cnpj.Invalid", "CNPJ inválido.", "cnpj"));

        return Result.Success(new Cnpj(DocumentValidation.OnlyNumbers(value)));
    }

    public static Cnpj FromTrusted(string value) => new(value);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
