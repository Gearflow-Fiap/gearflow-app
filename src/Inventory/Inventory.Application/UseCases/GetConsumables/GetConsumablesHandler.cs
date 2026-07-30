using Inventory.Application.Abstractions;
using Inventory.Application.DTOs;
using Shared.Domain.Primitives;

namespace Inventory.Application.UseCases.GetConsumables;

internal sealed class GetConsumablesHandler : IQueryHandler<GetConsumablesQuery, IReadOnlyList<ConsumableDto>>
{
    private readonly IConsumableRepository _repository;

    public GetConsumablesHandler(IConsumableRepository repository) => _repository = repository;

    public async Task<Result<IReadOnlyList<ConsumableDto>>> Handle(GetConsumablesQuery query, CancellationToken ct)
    {
        var consumables = await _repository.ListAsync(ct);
        IReadOnlyList<ConsumableDto> dtos = consumables.Select(ConsumableDto.FromAggregate).ToList();
        return Result.Success(dtos);
    }
}
