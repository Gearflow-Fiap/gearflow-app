using FluentAssertions;
using Inventory.Domain.Aggregates;

namespace Inventory.UnitTests;

public sealed class PartStockTests
{
    private static Part APart(int quantity) =>
        Part.Create("Filtro", "óleo", "PN-1", "Bosch", 5000, quantity).Value;

    [Fact]
    public void Reserve_moves_from_available_to_reserved()
    {
        var part = APart(10);

        var result = part.Reserve(4);

        result.IsSuccess.Should().BeTrue();
        part.Quantity.Should().Be(6);
        part.ReservedQuantity.Should().Be(4);
    }

    [Fact]
    public void Reserve_more_than_available_fails_with_conflict()
    {
        var part = APart(3);

        var result = part.Reserve(4);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Part.InsufficientStock");
        part.Quantity.Should().Be(3);
        part.ReservedQuantity.Should().Be(0);
    }

    [Fact]
    public void Consume_reduces_only_the_reservation()
    {
        var part = APart(10);
        part.Reserve(4);

        var result = part.Consume(4);

        result.IsSuccess.Should().BeTrue();
        part.Quantity.Should().Be(6);         // consumo não mexe no disponível — já saiu na reserva
        part.ReservedQuantity.Should().Be(0);
    }

    [Fact]
    public void Consume_more_than_reserved_fails()
    {
        var part = APart(10);
        part.Reserve(2);

        var result = part.Consume(3);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Part.InsufficientReserved");
    }

    [Fact]
    public void IsStockBelowMinimum_true_at_or_below_five()
    {
        APart(5).IsStockBelowMinimum().Should().BeTrue();
        APart(6).IsStockBelowMinimum().Should().BeFalse();
    }

    [Fact]
    public void AddQuantity_replenishes_available()
    {
        var part = APart(2);

        part.AddQuantity(8).IsSuccess.Should().BeTrue();

        part.Quantity.Should().Be(10);
    }

    [Fact]
    public void Create_trims_name_and_description_and_succeeds()
    {
        var result = Part.Create("  Filtro  ", "  óleo  ", "PN-1", "Bosch", 5000, 10);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Filtro");
        result.Value.Description.Should().Be("óleo");
        result.Value.ReservedQuantity.Should().Be(0);
    }

    [Fact]
    public void Create_with_null_description_defaults_to_empty()
    {
        Part.Create("Filtro", null!, "PN-1", "Bosch", 5000, 10).Value.Description.Should().BeEmpty();
    }

    [Theory]
    [InlineData("", "Part.NameRequired")]
    [InlineData("   ", "Part.NameRequired")]
    public void Create_without_name_fails(string name, string expectedCode)
    {
        Part.Create(name, "d", "PN-1", "Bosch", 5000, 10).Error.Code.Should().Be(expectedCode);
    }

    [Fact]
    public void Create_with_negative_price_fails()
    {
        Part.Create("Filtro", "d", "PN-1", "Bosch", -1, 10).Error.Code.Should().Be("Part.PriceNegative");
    }

    [Fact]
    public void Create_with_negative_quantity_fails()
    {
        Part.Create("Filtro", "d", "PN-1", "Bosch", 5000, -1).Error.Code.Should().Be("Part.QuantityNegative");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public void Reserve_with_non_positive_quantity_fails(int quantity)
    {
        APart(10).Reserve(quantity).Error.Code.Should().Be("Part.ReserveQuantityInvalid");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public void Consume_with_non_positive_quantity_fails(int quantity)
    {
        var part = APart(10);
        part.Reserve(4);

        part.Consume(quantity).Error.Code.Should().Be("Part.ConsumeQuantityInvalid");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddQuantity_with_non_positive_fails(int quantity)
    {
        APart(2).AddQuantity(quantity).Error.Code.Should().Be("Part.AddQuantityInvalid");
    }

    [Fact]
    public void HasAvailability_reflects_available_stock()
    {
        var part = APart(10);

        part.HasAvailability(10).Should().BeTrue();
        part.HasAvailability(11).Should().BeFalse();
    }

    [Fact]
    public void UpdateDetails_overwrites_fields()
    {
        var part = APart(10);

        part.UpdateDetails("Correia", "dentada", "PN-2", "Gates", 7000, 20);

        part.Name.Should().Be("Correia");
        part.Description.Should().Be("dentada");
        part.PartNumber.Should().Be("PN-2");
        part.Manufacturer.Should().Be("Gates");
        part.PriceCents.Should().Be(7000);
        part.Quantity.Should().Be(20);
    }
}
