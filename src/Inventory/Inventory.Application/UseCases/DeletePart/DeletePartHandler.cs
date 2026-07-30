using Inventory.Application.Abstractions;
using Inventory.Domain.ValueObjects;
using Shared.Domain.Primitives;

namespace Inventory.Application.UseCases.DeletePart;

internal sealed class DeletePartHandler : ICommandHandler<DeletePartCommand>
{
    private readonly IPartRepository _repository;

    public DeletePartHandler(IPartRepository repository) => _repository = repository;

    public async Task<Result> Handle(DeletePartCommand command, CancellationToken ct)
    {
        var part = await _repository.GetByIdAsync(PartId.From(command.PartId), ct);
        if (part is null)
            return Result.Failure(Error.NotFound("Part.NotFound", "Peça não encontrada."));

        _repository.Remove(part);
        await _repository.SaveChangesAsync(ct);
        return Result.Success();
    }
}
