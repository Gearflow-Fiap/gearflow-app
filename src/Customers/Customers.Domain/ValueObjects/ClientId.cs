using Shared.Domain.Primitives;

namespace Customers.Domain.ValueObjects;

public sealed class ClientId : ValueObject
{
    public Guid Value { get; }

    private ClientId(Guid value) => Value = value;

    public static ClientId New() => new(Guid.NewGuid());
    public static ClientId From(Guid value) => new(value);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}
