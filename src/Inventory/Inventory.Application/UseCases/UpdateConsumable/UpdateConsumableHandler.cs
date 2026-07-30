using Inventory.Application.Abstractions;
using Inventory.Application.DTOs;
using Inventory.Domain.ValueObjects;
using Shared.Domain.Primitives;

namespace Inventory.Application.UseCases.UpdateConsumable;

internal sealed class UpdateConsumableHandler : ICommandHandler<UpdateConsumableCommand, ConsumableDto>
{
    private readonly IConsumableRepository _repository;

    public UpdateConsumableHandler(IConsumableRepository repository) => _repository = repository;

    public async Task<Result<ConsumableDto>> Handle(UpdateConsumableCommand command, CancellationToken ct)
    {
        var consumable = await _repository.GetByIdAsync(ConsumableId.From(command.ConsumableId), ct);
        if (consumable is null)
            return Result.Failure<ConsumableDto>(Error.NotFound("Consumable.NotFound", "Insumo não encontrado."));

        consumable.UpdateDetails(command.Name, command.UnitPriceCents, command.Quantity);

        await _repository.SaveChangesAsync(ct);
        return Result.Success(ConsumableDto.FromAggregate(consumable));
    }
}
