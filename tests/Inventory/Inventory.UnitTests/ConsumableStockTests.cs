using FluentAssertions;
using Inventory.Domain.Aggregates;

namespace Inventory.UnitTests;

public sealed class ConsumableStockTests
{
    private static Consumable AConsumable(decimal quantity) =>
        Consumable.Create("Óleo 5W30", 3000, quantity).Value;

    // ---- Create -------------------------------------------------------------

    [Fact]
    public void Create_trims_name_and_succeeds()
    {
        var result = Consumable.Create("  Óleo  ", 3000, 10m);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Óleo");
        result.Value.Quantity.Should().Be(10m);
        result.Value.ReservedQuantity.Should().Be(0m);
    }

    [Theory]
    [InlineData("", "Consumable.NameRequired")]
    [InlineData("   ", "Consumable.NameRequired")]
    public void Create_without_name_fails(string name, string expectedCode)
    {
        var result = Consumable.Create(name, 3000, 10m);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(expectedCode);
    }

    [Fact]
    public void Create_with_negative_price_fails()
    {
        Consumable.Create("Óleo", -1, 10m).Error.Code.Should().Be("Consumable.PriceNegative");
    }

    [Fact]
    public void Create_with_negative_quantity_fails()
    {
        Consumable.Create("Óleo", 3000, -1m).Error.Code.Should().Be("Consumable.QuantityNegative");
    }

    // ---- Reserve ------------------------------------------------------------

    [Fact]
    public void Reserve_moves_from_available_to_reserved()
    {
        var consumable = AConsumable(10m);

        var result = consumable.Reserve(4m);

        result.IsSuccess.Should().BeTrue();
        consumable.Quantity.Should().Be(6m);
        consumable.ReservedQuantity.Should().Be(4m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public void Reserve_with_non_positive_quantity_fails(decimal quantity)
    {
        var result = AConsumable(10m).Reserve(quantity);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Consumable.ReserveQuantityInvalid");
    }

    [Fact]
    public void Reserve_more_than_available_fails_with_conflict()
    {
        var consumable = AConsumable(3m);

        var result = consumable.Reserve(4m);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Consumable.InsufficientStock");
        consumable.Quantity.Should().Be(3m);
        consumable.ReservedQuantity.Should().Be(0m);
    }

    // ---- Consume ------------------------------------------------------------

    [Fact]
    public void Consume_reduces_only_the_reservation()
    {
        var consumable = AConsumable(10m);
        consumable.Reserve(4m);

        var result = consumable.Consume(4m);

        result.IsSuccess.Should().BeTrue();
        consumable.Quantity.Should().Be(6m);      // consumo não mexe no disponível — saiu na reserva
        consumable.ReservedQuantity.Should().Be(0m);
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

    // ---- AddQuantity --------------------------------------------------------

    [Fact]
    public void AddQuantity_replenishes_available()
    {
        var consumable = AConsumable(2m);

        consumable.AddQuantity(8m).IsSuccess.Should().BeTrue();

        consumable.Quantity.Should().Be(10m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddQuantity_with_non_positive_fails(decimal quantity)
    {
        AConsumable(2m).AddQuantity(quantity).Error.Code.Should().Be("Consumable.AddQuantityInvalid");
    }

    // ---- Consultas + UpdateDetails -----------------------------------------

    [Fact]
    public void IsStockBelowMinimum_true_at_or_below_five()
    {
        AConsumable(5m).IsStockBelowMinimum().Should().BeTrue();
        AConsumable(6m).IsStockBelowMinimum().Should().BeFalse();
    }

    [Fact]
    public void HasAvailability_reflects_available_stock()
    {
        var consumable = AConsumable(10m);

        consumable.HasAvailability(10m).Should().BeTrue();
        consumable.HasAvailability(11m).Should().BeFalse();
    }

    [Fact]
    public void UpdateDetails_overwrites_fields()
    {
        var consumable = AConsumable(10m);

        consumable.UpdateDetails("Aditivo", 4200, 25m);

        consumable.Name.Should().Be("Aditivo");
        consumable.UnitPriceCents.Should().Be(4200);
        consumable.Quantity.Should().Be(25m);
    }
}
