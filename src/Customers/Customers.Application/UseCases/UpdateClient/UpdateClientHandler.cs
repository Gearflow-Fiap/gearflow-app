using Customers.Application.Abstractions;
using Customers.Application.DTOs;
using Customers.Domain.Aggregates;
using Customers.Domain.ValueObjects;
using Shared.Domain.Primitives;

namespace Customers.Application.UseCases.UpdateClient;

internal sealed class UpdateClientHandler : ICommandHandler<UpdateClientCommand, ClientDto>
{
    private readonly IClientRepository _repository;

    public UpdateClientHandler(IClientRepository repository) => _repository = repository;

    public async Task<Result<ClientDto>> Handle(UpdateClientCommand command, CancellationToken ct)
    {
        var client = await _repository.GetByIdAsync(ClientId.From(command.ClientId), ct);
        if (client is null)
            return Result.Failure<ClientDto>(Error.NotFound("Client.NotFound", "Cliente não encontrado."));

        var address = new Address(
            command.Address.Street, command.Address.City, command.Address.State,
            command.Address.Country, command.Address.ZipCode);

        var result = client.UpdateInformations(command.Name, command.Email, command.Phone, address);
        if (result.IsFailure)
            return Result.Failure<ClientDto>(result.Error);

        await _repository.SaveChangesAsync(ct);
        return Result.Success(ClientDto.FromAggregate(client));
    }
}
