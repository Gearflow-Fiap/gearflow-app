using Inventory.Application.Abstractions;
using Inventory.Application.DTOs;
using Inventory.Domain.ValueObjects;
using MediatR;
using Shared.Contracts.IntegrationEvents.Inventory;
using Shared.Domain.Primitives;

namespace Inventory.Application.UseCases.AddConsumableStock;

internal sealed class AddConsumableStockHandler : ICommandHandler<AddConsumableStockCommand, ConsumableDto>
{
    private readonly IConsumableRepository _repository;
    private readonly IPublisher _publisher;
    private readonly TimeProvider _timeProvider;

    public AddConsumableStockHandler(IConsumableRepository repository, IPublisher publisher, TimeProvider timeProvider)
    {
        _repository = repository;
        _publisher = publisher;
        _timeProvider = timeProvider;
    }

    public async Task<Result<ConsumableDto>> Handle(AddConsumableStockCommand command, CancellationToken ct)
    {
        var consumable = await _repository.GetByIdAsync(ConsumableId.From(command.ConsumableId), ct);
        if (consumable is null)
            return Result.Failure<ConsumableDto>(Error.NotFound("Consumable.NotFound", "Insumo não encontrado."));

        var result = consumable.AddQuantity(command.Quantity);
        if (result.IsFailure)
            return Result.Failure<ConsumableDto>(result.Error);

        await _repository.SaveChangesAsync(ct);

        await _publisher.Publish(
            new PartsReplenishedIntegrationEvent(Guid.NewGuid(), _timeProvider.GetUtcNow().UtcDateTime, "Consumable", consumable.Id.Value),
            ct);

        return Result.Success(ConsumableDto.FromAggregate(consumable));
    }
}
