using Customers.Domain.Common;
using Shared.Domain.Primitives;

namespace Customers.Domain.ValueObjects;

/// <summary>CPF do cliente (só dígitos). Regra de validação preservada do GearFlow.</summary>
public sealed class Cpf : ValueObject
{
    public string Value { get; }

    private Cpf(string value) => Value = value;

    public static Result<Cpf> Create(string value)
    {
        if (!DocumentValidation.IsValidCpf(value))
            return Result.Failure<Cpf>(Error.Validation("Cpf.Invalid", "CPF inválido.", "cpf"));

        return Result.Success(new Cpf(DocumentValidation.OnlyNumbers(value)));
    }

    /// <summary>Reidrata do banco (valor já validado na escrita).</summary>
    public static Cpf FromTrusted(string value) => new(value);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
