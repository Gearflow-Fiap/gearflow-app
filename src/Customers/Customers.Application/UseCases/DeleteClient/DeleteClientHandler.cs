using Customers.Application.Abstractions;
using Customers.Domain.ValueObjects;
using Shared.Domain.Primitives;

namespace Customers.Application.UseCases.DeleteClient;

internal sealed class DeleteClientHandler : ICommandHandler<DeleteClientCommand>
{
    private readonly IClientRepository _repository;

    public DeleteClientHandler(IClientRepository repository) => _repository = repository;

    public async Task<Result> Handle(DeleteClientCommand command, CancellationToken ct)
    {
        var client = await _repository.GetByIdAsync(ClientId.From(command.ClientId), ct);
        if (client is null)
            return Result.Failure(Error.NotFound("Client.NotFound", "Cliente não encontrado."));

        _repository.Remove(client);   // cascade remove dos veículos (OnDelete Cascade)
        await _repository.SaveChangesAsync(ct);
        return Result.Success();
    }
}
