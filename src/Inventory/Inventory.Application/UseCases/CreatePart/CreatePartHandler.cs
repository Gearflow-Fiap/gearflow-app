using Inventory.Application.Abstractions;
using Inventory.Application.DTOs;
using Inventory.Domain.Aggregates;
using Shared.Domain.Primitives;

namespace Inventory.Application.UseCases.CreatePart;

internal sealed class CreatePartHandler : ICommandHandler<CreatePartCommand, PartDto>
{
    private readonly IPartRepository _repository;

    public CreatePartHandler(IPartRepository repository) => _repository = repository;

    public async Task<Result<PartDto>> Handle(CreatePartCommand command, CancellationToken ct)
    {
        var result = Part.Create(command.Name, command.Description, command.PartNumber,
            command.Manufacturer, command.PriceCents, command.Quantity);
        if (result.IsFailure)
            return Result.Failure<PartDto>(result.Error);

        await _repository.AddAsync(result.Value, ct);
        await _repository.SaveChangesAsync(ct);

        return Result.Success(PartDto.FromAggregate(result.Value));
    }
}
