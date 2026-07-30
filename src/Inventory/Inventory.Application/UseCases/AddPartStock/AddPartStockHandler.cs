using Inventory.Application.Abstractions;
using Inventory.Application.DTOs;
using Inventory.Domain.ValueObjects;
using MediatR;
using Shared.Contracts;
using Shared.Contracts.IntegrationEvents.Inventory;
using Shared.Domain.Primitives;

namespace Inventory.Application.UseCases.AddPartStock;

/// <summary>
/// Repõe estoque de uma peça (<c>PATCH stock</c> do GearFlow). Ao repor, publica
/// <see cref="PartsReplenishedIntegrationEvent"/> para o Workshop reavaliar OS paradas em
/// <c>AwaitingPartsOrConsumables</c> (auto-resume) — preserva a regra original como evento cross-BC.
/// </summary>
internal sealed class AddPartStockHandler : ICommandHandler<AddPartStockCommand, PartDto>
{
    private readonly IPartRepository _repository;
    private readonly IPublisher _publisher;
    private readonly TimeProvider _timeProvider;

    public AddPartStockHandler(IPartRepository repository, IPublisher publisher, TimeProvider timeProvider)
    {
        _repository = repository;
        _publisher = publisher;
        _timeProvider = timeProvider;
    }

    public async Task<Result<PartDto>> Handle(AddPartStockCommand command, CancellationToken ct)
    {
        var part = await _repository.GetByIdAsync(PartId.From(command.PartId), ct);
        if (part is null)
            return Result.Failure<PartDto>(Error.NotFound("Part.NotFound", "Peça não encontrada."));

        var result = part.AddQuantity(command.Quantity);
        if (result.IsFailure)
            return Result.Failure<PartDto>(result.Error);

        await _repository.SaveChangesAsync(ct);

        await _publisher.Publish(
            new PartsReplenishedIntegrationEvent(Guid.NewGuid(), _timeProvider.GetUtcNow().UtcDateTime, InventoryItemType.Part, part.Id.Value),
            ct);

        return Result.Success(PartDto.FromAggregate(part));
    }
}
