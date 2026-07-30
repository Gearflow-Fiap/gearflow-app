using Shared.Domain.Primitives;

namespace Identity.Domain.ValueObjects;

public sealed class UserId : ValueObject
{
    public Guid Value { get; }
    private UserId(Guid value) => Value = value;
    public static UserId New() => new(Guid.NewGuid());
    public static UserId From(Guid value) => new(value);
    protected override IEnumerable<object> GetEqualityComponents() { yield return Value; }
    public override string ToString() => Value.ToString();
}
