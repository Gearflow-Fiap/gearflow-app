using Inventory.Application.Abstractions;
using Inventory.Application.DTOs;
using Inventory.Domain.Aggregates;
using Shared.Domain.Primitives;

namespace Inventory.Application.UseCases.CreateConsumable;

internal sealed class CreateConsumableHandler : ICommandHandler<CreateConsumableCommand, ConsumableDto>
{
    private readonly IConsumableRepository _repository;

    public CreateConsumableHandler(IConsumableRepository repository) => _repository = repository;

    public async Task<Result<ConsumableDto>> Handle(CreateConsumableCommand command, CancellationToken ct)
    {
        var result = Consumable.Create(command.Name, command.UnitPriceCents, command.Quantity);
        if (result.IsFailure)
            return Result.Failure<ConsumableDto>(result.Error);

        await _repository.AddAsync(result.Value, ct);
        await _repository.SaveChangesAsync(ct);

        return Result.Success(ConsumableDto.FromAggregate(result.Value));
    }
}
