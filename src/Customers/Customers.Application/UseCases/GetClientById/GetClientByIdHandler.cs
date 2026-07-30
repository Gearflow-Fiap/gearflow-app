using Customers.Application.Abstractions;
using Customers.Application.DTOs;
using Customers.Domain.ValueObjects;
using Shared.Domain.Primitives;

namespace Customers.Application.UseCases.GetClientById;

internal sealed class GetClientByIdHandler : IQueryHandler<GetClientByIdQuery, ClientDto>
{
    private readonly IClientRepository _repository;

    public GetClientByIdHandler(IClientRepository repository) => _repository = repository;

    public async Task<Result<ClientDto>> Handle(GetClientByIdQuery query, CancellationToken ct)
    {
        var client = await _repository.GetByIdAsync(ClientId.From(query.ClientId), ct);
        if (client is null)
            return Result.Failure<ClientDto>(Error.NotFound("Client.NotFound", "Cliente não encontrado."));

        return Result.Success(ClientDto.FromAggregate(client));
    }
}
