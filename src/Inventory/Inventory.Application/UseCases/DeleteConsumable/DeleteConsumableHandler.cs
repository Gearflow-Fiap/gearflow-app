using Inventory.Application.Abstractions;
using Inventory.Domain.ValueObjects;
using Shared.Domain.Primitives;

namespace Inventory.Application.UseCases.DeleteConsumable;

internal sealed class DeleteConsumableHandler : ICommandHandler<DeleteConsumableCommand>
{
    private readonly IConsumableRepository _repository;

    public DeleteConsumableHandler(IConsumableRepository repository) => _repository = repository;

    public async Task<Result> Handle(DeleteConsumableCommand command, CancellationToken ct)
    {
        var consumable = await _repository.GetByIdAsync(ConsumableId.From(command.ConsumableId), ct);
        if (consumable is null)
            return Result.Failure(Error.NotFound("Consumable.NotFound", "Insumo não encontrado."));

        _repository.Remove(consumable);
        await _repository.SaveChangesAsync(ct);
        return Result.Success();
    }
}
