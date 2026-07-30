using Inventory.Application.Abstractions;
using Inventory.Application.DTOs;
using Inventory.Domain.ValueObjects;
using Shared.Domain.Primitives;

namespace Inventory.Application.UseCases.GetConsumableById;

internal sealed class GetConsumableByIdHandler : IQueryHandler<GetConsumableByIdQuery, ConsumableDto>
{
    private readonly IConsumableRepository _repository;

    public GetConsumableByIdHandler(IConsumableRepository repository) => _repository = repository;

    public async Task<Result<ConsumableDto>> Handle(GetConsumableByIdQuery query, CancellationToken ct)
    {
        var consumable = await _repository.GetByIdAsync(ConsumableId.From(query.ConsumableId), ct);
        if (consumable is null)
            return Result.Failure<ConsumableDto>(Error.NotFound("Consumable.NotFound", "Insumo não encontrado."));

        return Result.Success(ConsumableDto.FromAggregate(consumable));
    }
}
