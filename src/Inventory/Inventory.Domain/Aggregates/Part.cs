using Inventory.Domain.ValueObjects;
using Shared.Domain.Primitives;

namespace Inventory.Domain.Aggregates;

/// <summary>
/// Peça de estoque. Preserva o modelo bifásico do GearFlow: <see cref="Reserve"/> move de
/// disponível para reservado (na aprovação do orçamento) e <see cref="Consume"/> baixa a reserva
/// (na finalização da OS). Erros de regra agora via <see cref="Result"/> em vez de exceção.
/// </summary>
public sealed class Part : AggregateRoot<PartId>
{
    public const int MinimumStockQuantity = 5;

    public string Name { get; private set; }
    public string Description { get; private set; }
    public string PartNumber { get; private set; }
    public string Manufacturer { get; private set; }
    public int PriceCents { get; private set; }
    public int Quantity { get; private set; }
    public int ReservedQuantity { get; private set; }
    public DateTime? UpdatedOn { get; private set; }

    private Part(PartId id, string name, string description, string partNumber, string manufacturer, int priceCents, int quantity)
        : base(id)
    {
        Name = name;
        Description = description;
        PartNumber = partNumber;
        Manufacturer = manufacturer;
        PriceCents = priceCents;
        Quantity = quantity;
        ReservedQuantity = 0;
    }

    private Part() : base(PartId.New())
    {
        Name = null!;
        Description = null!;
        PartNumber = null!;
        Manufacturer = null!;
    }

    public static Result<Part> Create(
        string name, string description, string partNumber, string manufacturer, int priceCents, int quantity)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<Part>(Error.Validation("Part.NameRequired", "Nome da peça é obrigatório."));
        if (priceCents < 0)
            return Result.Failure<Part>(Error.Validation("Part.PriceNegative", "Preço não pode ser negativo."));
        if (quantity < 0)
            return Result.Failure<Part>(Error.Validation("Part.QuantityNegative", "Quantidade não pode ser negativa."));

        return Result.Success(new Part(PartId.New(), name.Trim(), description?.Trim() ?? string.Empty,
            partNumber, manufacturer, priceCents, quantity));
    }

    public bool IsStockBelowMinimum() => Quantity <= MinimumStockQuantity;

    public bool HasAvailability(int quantity) => Quantity >= quantity;

    public Result Reserve(int quantity)
    {
        if (quantity <= 0)
            return Result.Failure(Error.Validation("Part.ReserveQuantityInvalid", "A quantidade reservada deve ser maior que zero."));
        if (Quantity < quantity)
            return Result.Failure(Error.Conflict("Part.InsufficientStock",
                $"Não há disponibilidade no estoque da peça {PartNumber}. No estoque: {Quantity}, Reservadas: {ReservedQuantity}, Pedido: {quantity}"));

        Quantity -= quantity;
        ReservedQuantity += quantity;
        UpdatedOn = DateTime.UtcNow;
        return Result.Success();
    }

    public Result Consume(int quantity)
    {
        if (quantity <= 0)
            return Result.Failure(Error.Validation("Part.ConsumeQuantityInvalid", "A quantidade consumida deve ser maior que zero."));
        if (ReservedQuantity < quantity)
            return Result.Failure(Error.Conflict("Part.InsufficientReserved",
                $"Peça {PartNumber} não possui quantidade reservada suficiente. Reservado: {ReservedQuantity}, Solicitado: {quantity}"));

        ReservedQuantity -= quantity;
        UpdatedOn = DateTime.UtcNow;
        return Result.Success();
    }

    public Result AddQuantity(int quantity)
    {
        if (quantity <= 0)
            return Result.Failure(Error.Validation("Part.AddQuantityInvalid", "A quantidade a adicionar deve ser maior que zero."));

        Quantity += quantity;
        UpdatedOn = DateTime.UtcNow;
        return Result.Success();
    }

    public void UpdateDetails(string name, string description, string partNumber, string manufacturer, int priceCents, int quantity)
    {
        Name = name;
        Description = description;
        PartNumber = partNumber;
        Manufacturer = manufacturer;
        PriceCents = priceCents;
        Quantity = quantity;
        UpdatedOn = DateTime.UtcNow;
    }
}
