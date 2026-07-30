using Inventory.Domain.ValueObjects;
using Shared.Domain.Primitives;

namespace Inventory.Domain.Aggregates;

/// <summary>
/// Insumo de estoque (quantidade fracionária — ex.: litros). Mesmo modelo bifásico da
/// <see cref="Part"/>: reserva na aprovação, consumo na finalização.
/// </summary>
public sealed class Consumable : AggregateRoot<ConsumableId>
{
    public const decimal MinimumStockQuantity = 5m;

    public string Name { get; private set; }
    public int UnitPriceCents { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal ReservedQuantity { get; private set; }
    public DateTime UpdatedOn { get; private set; }

    private Consumable(ConsumableId id, string name, int unitPriceCents, decimal quantity)
        : base(id)
    {
        Name = name;
        UnitPriceCents = unitPriceCents;
        Quantity = quantity;
        UpdatedOn = DateTime.UtcNow;
    }

    private Consumable() : base(ConsumableId.New())
    {
        Name = null!;
    }

    public static Result<Consumable> Create(string name, int unitPriceCents, decimal quantity)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<Consumable>(Error.Validation("Consumable.NameRequired", "Nome do insumo é obrigatório."));
        if (unitPriceCents < 0)
            return Result.Failure<Consumable>(Error.Validation("Consumable.PriceNegative", "Preço não pode ser negativo."));
        if (quantity < 0)
            return Result.Failure<Consumable>(Error.Validation("Consumable.QuantityNegative", "Quantidade não pode ser negativa."));

        return Result.Success(new Consumable(ConsumableId.New(), name.Trim(), unitPriceCents, quantity));
    }

    public bool IsStockBelowMinimum() => Quantity <= MinimumStockQuantity;

    public bool HasAvailability(decimal quantity) => Quantity >= quantity;

    public Result Reserve(decimal quantity)
    {
        if (quantity <= 0)
            return Result.Failure(Error.Validation("Consumable.ReserveQuantityInvalid", "A quantidade reservada deve ser maior que zero."));
        if (Quantity < quantity)
            return Result.Failure(Error.Conflict("Consumable.InsufficientStock",
                $"Não há disponibilidade no estoque do insumo {Name}. No estoque: {Quantity}, Reservadas: {ReservedQuantity}, Solicitado: {quantity}"));

        Quantity -= quantity;
        ReservedQuantity += quantity;
        UpdatedOn = DateTime.UtcNow;
        return Result.Success();
    }

    public Result Consume(decimal quantity)
    {
        if (ReservedQuantity < quantity)
            return Result.Failure(Error.Conflict("Consumable.InsufficientReserved",
                $"Insumo {Name} não possui quantidade reservada suficiente. Reservado: {ReservedQuantity}, Solicitado: {quantity}"));

        ReservedQuantity -= quantity;
        UpdatedOn = DateTime.UtcNow;
        return Result.Success();
    }

    public Result AddQuantity(decimal quantity)
    {
        if (quantity <= 0)
            return Result.Failure(Error.Validation("Consumable.AddQuantityInvalid", "A quantidade a adicionar deve ser maior que zero."));

        Quantity += quantity;
        UpdatedOn = DateTime.UtcNow;
        return Result.Success();
    }

    public void UpdateDetails(string name, int unitPriceCents, decimal quantity)
    {
        Name = name;
        UnitPriceCents = unitPriceCents;
        Quantity = quantity;
        UpdatedOn = DateTime.UtcNow;
    }
}
