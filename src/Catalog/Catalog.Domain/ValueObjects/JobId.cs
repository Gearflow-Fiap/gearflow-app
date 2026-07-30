using Shared.Domain.Primitives;

namespace Catalog.Domain.ValueObjects;

/// <summary>Id fortemente tipado do <see cref="Aggregates.Job"/> (serviço de mão de obra).</summary>
public sealed class JobId : ValueObject
{
    public Guid Value { get; }

    private JobId(Guid value) => Value = value;

    public static JobId New() => new(Guid.NewGuid());
    public static JobId From(Guid value) => new(value);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}
