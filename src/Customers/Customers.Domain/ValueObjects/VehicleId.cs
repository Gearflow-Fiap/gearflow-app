using Shared.Domain.Primitives;

namespace Customers.Domain.ValueObjects;

public sealed class VehicleId : ValueObject
{
    public Guid Value { get; }

    private VehicleId(Guid value) => Value = value;

    public static VehicleId New() => new(Guid.NewGuid());
    public static VehicleId From(Guid value) => new(value);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}
