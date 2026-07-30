using Shared.Domain.Primitives;

namespace Workshop.Domain.ValueObjects;

public sealed class ServiceOrderId : ValueObject
{
    public Guid Value { get; }
    private ServiceOrderId(Guid value) => Value = value;
    public static ServiceOrderId New() => new(Guid.NewGuid());
    public static ServiceOrderId From(Guid value) => new(value);
    protected override IEnumerable<object> GetEqualityComponents() { yield return Value; }
    public override string ToString() => Value.ToString();
}
