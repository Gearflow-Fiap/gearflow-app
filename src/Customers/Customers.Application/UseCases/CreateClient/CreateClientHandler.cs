using Customers.Application.Abstractions;
using Customers.Application.DTOs;
using Customers.Domain.Aggregates;
using Customers.Domain.ValueObjects;
using Shared.Domain.Primitives;

namespace Customers.Application.UseCases.CreateClient;

internal sealed class CreateClientHandler : ICommandHandler<CreateClientCommand, ClientDto>
{
    private readonly IClientRepository _repository;

    public CreateClientHandler(IClientRepository repository) => _repository = repository;

    public async Task<Result<ClientDto>> Handle(CreateClientCommand command, CancellationToken ct)
    {
        var address = new Address(
            command.Address.Street, command.Address.City, command.Address.State,
            command.Address.Country, command.Address.ZipCode);

        var result = Client.Create(command.Cpf, command.Cnpj, command.Name, command.Email, command.Phone, address);
        if (result.IsFailure)
            return Result.Failure<ClientDto>(result.Error);

        await _repository.AddAsync(result.Value, ct);
        await _repository.SaveChangesAsync(ct);

        return Result.Success(ClientDto.FromAggregate(result.Value));
    }
}
