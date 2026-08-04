using FluentAssertions;
using Inventory.Application.UseCases.AddConsumableStock;
using Inventory.Application.UseCases.AddPartStock;
using Inventory.Application.UseCases.CreateConsumable;
using Inventory.Application.UseCases.CreatePart;
using Inventory.Application.UseCases.DeleteConsumable;
using Inventory.Application.UseCases.DeletePart;
using Inventory.Application.UseCases.GetConsumableById;
using Inventory.Application.UseCases.GetConsumables;
using Inventory.Application.UseCases.GetPartById;
using Inventory.Application.UseCases.GetParts;
using Inventory.Application.UseCases.UpdateConsumable;
using Inventory.Application.UseCases.UpdatePart;
using Inventory.Application.Abstractions;
using Inventory.Domain.Aggregates;
using MediatR;
using NSubstitute;

namespace Inventory.UnitTests;

/// <summary>
/// Handlers de Inventory.Application com repositórios/publisher fakes (NSubstitute). Cobre os dois
/// ramos de cada handler (sucesso e falha: NotFound / validação), fechando linha e branch.
/// </summary>
public sealed class InventoryHandlerTests
{
    private readonly IPartRepository _parts = Substitute.For<IPartRepository>();
    private readonly IConsumableRepository _consumables = Substitute.For<IConsumableRepository>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();

    private static Part APart(int qty = 10) =>
        Part.Create("Filtro", "óleo", "PN-1", "Bosch", 5000, qty).Value;

    private static Consumable AConsumable(decimal qty = 10m) =>
        Consumable.Create("Óleo 5W30", 3000, qty).Value;

    // ---- Part: Create -------------------------------------------------------

    [Fact]
    public async Task CreatePart_persists_and_returns_dto()
    {
        var result = await new CreatePartHandler(_parts)
            .Handle(new CreatePartCommand("Filtro", "óleo", "PN-1", "Bosch", 5000, 10), default);

        result.IsSuccess.Should().BeTrue();
        await _parts.Received(1).AddAsync(Arg.Any<Part>(), Arg.Any<CancellationToken>());
        await _parts.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreatePart_with_invalid_data_fails_and_does_not_persist()
    {
        var result = await new CreatePartHandler(_parts)
            .Handle(new CreatePartCommand("", "óleo", "PN-1", "Bosch", 5000, 10), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Part.NameRequired");
        await _parts.DidNotReceive().AddAsync(Arg.Any<Part>(), Arg.Any<CancellationToken>());
    }

    // ---- Part: Update / Delete / Get (ramo NotFound + sucesso) --------------

    [Fact]
    public async Task UpdatePart_updates_when_found()
    {
        var part = APart();
        _parts.GetByIdAsync(Arg.Any<Domain.ValueObjects.PartId>(), Arg.Any<CancellationToken>()).Returns(part);

        var result = await new UpdatePartHandler(_parts)
            .Handle(new UpdatePartCommand(part.Id.Value, "Correia", "d", "PN-2", "Gates", 7000, 20), default);

        result.IsSuccess.Should().BeTrue();
        await _parts.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdatePart_returns_not_found_when_missing()
    {
        _parts.GetByIdAsync(Arg.Any<Domain.ValueObjects.PartId>(), Arg.Any<CancellationToken>()).Returns((Part?)null);

        var result = await new UpdatePartHandler(_parts)
            .Handle(new UpdatePartCommand(Guid.NewGuid(), "x", "d", "PN", "M", 1, 1), default);

        result.Error.Code.Should().Be("Part.NotFound");
    }

    [Fact]
    public async Task DeletePart_removes_when_found()
    {
        var part = APart();
        _parts.GetByIdAsync(Arg.Any<Domain.ValueObjects.PartId>(), Arg.Any<CancellationToken>()).Returns(part);

        var result = await new DeletePartHandler(_parts).Handle(new DeletePartCommand(part.Id.Value), default);

        result.IsSuccess.Should().BeTrue();
        _parts.Received(1).Remove(part);
    }

    [Fact]
    public async Task DeletePart_returns_not_found_when_missing()
    {
        _parts.GetByIdAsync(Arg.Any<Domain.ValueObjects.PartId>(), Arg.Any<CancellationToken>()).Returns((Part?)null);

        (await new DeletePartHandler(_parts).Handle(new DeletePartCommand(Guid.NewGuid()), default))
            .Error.Code.Should().Be("Part.NotFound");
    }

    [Fact]
    public async Task GetPartById_returns_dto_or_not_found()
    {
        var part = APart();
        _parts.GetByIdAsync(Arg.Any<Domain.ValueObjects.PartId>(), Arg.Any<CancellationToken>()).Returns(part, (Part?)null);

        (await new GetPartByIdHandler(_parts).Handle(new GetPartByIdQuery(part.Id.Value), default))
            .IsSuccess.Should().BeTrue();
        (await new GetPartByIdHandler(_parts).Handle(new GetPartByIdQuery(Guid.NewGuid()), default))
            .Error.Code.Should().Be("Part.NotFound");
    }

    [Fact]
    public async Task GetParts_returns_list()
    {
        _parts.ListAsync(Arg.Any<CancellationToken>()).Returns(new[] { APart(), APart(3) });

        var result = await new GetPartsHandler(_parts).Handle(new GetPartsQuery(), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    // ---- Part: AddStock (sucesso publica evento; NotFound; qty inválida) ----

    [Fact]
    public async Task AddPartStock_replenishes_and_publishes_event()
    {
        var part = APart(2);
        _parts.GetByIdAsync(Arg.Any<Domain.ValueObjects.PartId>(), Arg.Any<CancellationToken>()).Returns(part);

        var result = await new AddPartStockHandler(_parts, _publisher, TimeProvider.System)
            .Handle(new AddPartStockCommand(part.Id.Value, 8), default);

        result.IsSuccess.Should().BeTrue();
        part.Quantity.Should().Be(10);
        await _publisher.Received(1).Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddPartStock_returns_not_found_when_missing()
    {
        _parts.GetByIdAsync(Arg.Any<Domain.ValueObjects.PartId>(), Arg.Any<CancellationToken>()).Returns((Part?)null);

        var result = await new AddPartStockHandler(_parts, _publisher, TimeProvider.System)
            .Handle(new AddPartStockCommand(Guid.NewGuid(), 8), default);

        result.Error.Code.Should().Be("Part.NotFound");
        await _publisher.DidNotReceive().Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddPartStock_with_invalid_quantity_fails()
    {
        var part = APart();
        _parts.GetByIdAsync(Arg.Any<Domain.ValueObjects.PartId>(), Arg.Any<CancellationToken>()).Returns(part);

        var result = await new AddPartStockHandler(_parts, _publisher, TimeProvider.System)
            .Handle(new AddPartStockCommand(part.Id.Value, 0), default);

        result.Error.Code.Should().Be("Part.AddQuantityInvalid");
    }

    // ---- Consumable: espelha os mesmos ramos --------------------------------

    [Fact]
    public async Task CreateConsumable_persists_or_fails_on_invalid()
    {
        (await new CreateConsumableHandler(_consumables)
            .Handle(new CreateConsumableCommand("Óleo", 3000, 10m), default)).IsSuccess.Should().BeTrue();

        (await new CreateConsumableHandler(_consumables)
            .Handle(new CreateConsumableCommand(" ", 3000, 10m), default)).Error.Code.Should().Be("Consumable.NameRequired");
    }

    [Fact]
    public async Task UpdateConsumable_updates_or_not_found()
    {
        var c = AConsumable();
        _consumables.GetByIdAsync(Arg.Any<Domain.ValueObjects.ConsumableId>(), Arg.Any<CancellationToken>()).Returns(c, (Consumable?)null);

        (await new UpdateConsumableHandler(_consumables)
            .Handle(new UpdateConsumableCommand(c.Id.Value, "Aditivo", 4200, 25m), default)).IsSuccess.Should().BeTrue();
        (await new UpdateConsumableHandler(_consumables)
            .Handle(new UpdateConsumableCommand(Guid.NewGuid(), "x", 1, 1m), default)).Error.Code.Should().Be("Consumable.NotFound");
    }

    [Fact]
    public async Task DeleteConsumable_removes_or_not_found()
    {
        var c = AConsumable();
        _consumables.GetByIdAsync(Arg.Any<Domain.ValueObjects.ConsumableId>(), Arg.Any<CancellationToken>()).Returns(c, (Consumable?)null);

        (await new DeleteConsumableHandler(_consumables).Handle(new DeleteConsumableCommand(c.Id.Value), default)).IsSuccess.Should().BeTrue();
        _consumables.Received(1).Remove(c);
        (await new DeleteConsumableHandler(_consumables).Handle(new DeleteConsumableCommand(Guid.NewGuid()), default)).Error.Code.Should().Be("Consumable.NotFound");
    }

    [Fact]
    public async Task GetConsumableById_returns_dto_or_not_found()
    {
        var c = AConsumable();
        _consumables.GetByIdAsync(Arg.Any<Domain.ValueObjects.ConsumableId>(), Arg.Any<CancellationToken>()).Returns(c, (Consumable?)null);

        (await new GetConsumableByIdHandler(_consumables).Handle(new GetConsumableByIdQuery(c.Id.Value), default)).IsSuccess.Should().BeTrue();
        (await new GetConsumableByIdHandler(_consumables).Handle(new GetConsumableByIdQuery(Guid.NewGuid()), default)).Error.Code.Should().Be("Consumable.NotFound");
    }

    [Fact]
    public async Task GetConsumables_returns_list()
    {
        _consumables.ListAsync(Arg.Any<CancellationToken>()).Returns(new[] { AConsumable(), AConsumable(3m) });

        (await new GetConsumablesHandler(_consumables).Handle(new GetConsumablesQuery(), default)).Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task AddConsumableStock_replenishes_publishes_or_not_found()
    {
        var c = AConsumable(2m);
        _consumables.GetByIdAsync(Arg.Any<Domain.ValueObjects.ConsumableId>(), Arg.Any<CancellationToken>()).Returns(c, (Consumable?)null);

        var ok = await new AddConsumableStockHandler(_consumables, _publisher, TimeProvider.System)
            .Handle(new AddConsumableStockCommand(c.Id.Value, 8m), default);
        ok.IsSuccess.Should().BeTrue();
        c.Quantity.Should().Be(10m);
        await _publisher.Received(1).Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>());

        var missing = await new AddConsumableStockHandler(_consumables, _publisher, TimeProvider.System)
            .Handle(new AddConsumableStockCommand(Guid.NewGuid(), 8m), default);
        missing.Error.Code.Should().Be("Consumable.NotFound");
    }
}
