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
}
