using Customers.Application.Abstractions;
using Customers.Application.DTOs;
using Shared.Domain.Primitives;

namespace Customers.Application.UseCases.GetClients;

internal sealed class GetClientsHandler : IQueryHandler<GetClientsQuery, IReadOnlyList<ClientDto>>
{
    private readonly IClientRepository _repository;

    public GetClientsHandler(IClientRepository repository) => _repository = repository;

    public async Task<Result<IReadOnlyList<ClientDto>>> Handle(GetClientsQuery query, CancellationToken ct)
    {
        var clients = await _repository.ListAsync(ct);
        IReadOnlyList<ClientDto> dtos = clients.Select(ClientDto.FromAggregate).ToList();
        return Result.Success(dtos);
    }
}
