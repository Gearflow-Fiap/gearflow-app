using Inventory.Application.Abstractions;
using Inventory.Application.DTOs;
using Shared.Domain.Primitives;

namespace Inventory.Application.UseCases.GetParts;

internal sealed class GetPartsHandler : IQueryHandler<GetPartsQuery, IReadOnlyList<PartDto>>
{
    private readonly IPartRepository _repository;

    public GetPartsHandler(IPartRepository repository) => _repository = repository;

    public async Task<Result<IReadOnlyList<PartDto>>> Handle(GetPartsQuery query, CancellationToken ct)
    {
        var parts = await _repository.ListAsync(ct);
        IReadOnlyList<PartDto> dtos = parts.Select(PartDto.FromAggregate).ToList();
        return Result.Success(dtos);
    }
}
