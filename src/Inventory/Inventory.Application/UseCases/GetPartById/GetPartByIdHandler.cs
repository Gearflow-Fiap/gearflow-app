using Inventory.Application.Abstractions;
using Inventory.Application.DTOs;
using Inventory.Domain.ValueObjects;
using Shared.Domain.Primitives;

namespace Inventory.Application.UseCases.GetPartById;

internal sealed class GetPartByIdHandler : IQueryHandler<GetPartByIdQuery, PartDto>
{
    private readonly IPartRepository _repository;

    public GetPartByIdHandler(IPartRepository repository) => _repository = repository;

    public async Task<Result<PartDto>> Handle(GetPartByIdQuery query, CancellationToken ct)
    {
        var part = await _repository.GetByIdAsync(PartId.From(query.PartId), ct);
        if (part is null)
            return Result.Failure<PartDto>(Error.NotFound("Part.NotFound", "Peça não encontrada."));

        return Result.Success(PartDto.FromAggregate(part));
    }
}
