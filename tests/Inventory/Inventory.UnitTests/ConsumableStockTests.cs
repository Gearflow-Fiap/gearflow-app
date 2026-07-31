using FluentAssertions;
using Inventory.Domain.Aggregates;

namespace Inventory.UnitTests;

/// <summary>
/// Estoque bifásico do insumo (<see cref="Consumable"/>) — mesma regra da peça, mas com quantidade
/// <b>fracionária</b> (litros). Complementa PartStockTests (que cobre o inteiro).
/// </summary>
public sealed class ConsumableStockTests
{
    private static Consumable AConsumable(decimal quantity) =>
        Consumable.Create("Óleo 5W30", 4500, quantity).Value;

    [Fact]
    public void Create_rejects_negative_price_and_quantity()
    {
        Consumable.Create("Óleo", -1, 10m).IsFailure.Should().BeTrue();
        Consumable.Create("Óleo", 100, -1m).IsFailure.Should().BeTrue();
        Consumable.Create(" ", 100, 10m).Error.Code.Should().Be("Consumable.NameRequired");
    }

    [Fact]
    public void Reserve_moves_fractional_quantity_from_available_to_reserved()
    {
        var consumable = AConsumable(10.5m);

        var result = consumable.Reserve(2.5m);

        result.IsSuccess.Should().BeTrue();
        consumable.Quantity.Should().Be(8.0m);
        consumable.ReservedQuantity.Should().Be(2.5m);
    }

    [Fact]
    public void Reserve_more_than_available_fails_with_conflict()
    {
        var consumable = AConsumable(1m);

        var result = consumable.Reserve(2m);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Consumable.InsufficientStock");
        consumable.Quantity.Should().Be(1m);
    }

    [Fact]
    public void Reserve_non_positive_quantity_is_invalid()
    {
        AConsumable(10m).Reserve(0m).Error.Code.Should().Be("Consumable.ReserveQuantityInvalid");
    }

    [Fact]
    public void Consume_reduces_only_the_reservation()
    {
        var consumable = AConsumable(10m);
        consumable.Reserve(4m); // disponível 6, reservado 4

        var result = consumable.Consume(4m);

        result.IsSuccess.Should().BeTrue();
        consumable.Quantity.Should().Be(6m);           // disponível intacto
        consumable.ReservedQuantity.Should().Be(0m);   // baixou só a reserva
    }

    [Fact]
    public void Consume_more_than_reserved_fails()
    {
        var consumable = AConsumable(10m);
        consumable.Reserve(2m);

        var result = consumable.Consume(3m);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Consumable.InsufficientReserved");
    }

    [Theory]
    [InlineData(5.0, true)]     // no mínimo
    [InlineData(4.9, true)]     // abaixo do mínimo
    [InlineData(5.1, false)]    // acima
    public void IsStockBelowMinimum_at_or_below_five(decimal quantity, bool expected)
    {
        AConsumable(quantity).IsStockBelowMinimum().Should().Be(expected);
    }

    [Fact]
    public void AddQuantity_replenishes_available()
    {
        var consumable = AConsumable(2m);

        consumable.AddQuantity(8.5m).IsSuccess.Should().BeTrue();

        consumable.Quantity.Should().Be(10.5m);
        consumable.AddQuantity(0m).Error.Code.Should().Be("Consumable.AddQuantityInvalid");
    }
}
