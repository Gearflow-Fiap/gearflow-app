using Inventory.Application.Abstractions;
using Inventory.Application.DTOs;
using Inventory.Domain.ValueObjects;
using Shared.Domain.Primitives;

namespace Inventory.Application.UseCases.UpdatePart;

internal sealed class UpdatePartHandler : ICommandHandler<UpdatePartCommand, PartDto>
{
    private readonly IPartRepository _repository;

    public UpdatePartHandler(IPartRepository repository) => _repository = repository;

    public async Task<Result<PartDto>> Handle(UpdatePartCommand command, CancellationToken ct)
    {
        var part = await _repository.GetByIdAsync(PartId.From(command.PartId), ct);
        if (part is null)
            return Result.Failure<PartDto>(Error.NotFound("Part.NotFound", "Peça não encontrada."));

        part.UpdateDetails(command.Name, command.Description, command.PartNumber,
            command.Manufacturer, command.PriceCents, command.Quantity);

        await _repository.SaveChangesAsync(ct);
        return Result.Success(PartDto.FromAggregate(part));
    }
}
