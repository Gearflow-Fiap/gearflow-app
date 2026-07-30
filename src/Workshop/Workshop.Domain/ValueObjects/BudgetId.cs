using Shared.Domain.Primitives;

namespace Workshop.Domain.ValueObjects;

public sealed class BudgetId : ValueObject
{
    public Guid Value { get; }
    private BudgetId(Guid value) => Value = value;
    public static BudgetId New() => new(Guid.NewGuid());
    public static BudgetId From(Guid value) => new(value);
    protected override IEnumerable<object> GetEqualityComponents() { yield return Value; }
    public override string ToString() => Value.ToString();
}
