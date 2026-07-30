using Customers.Application.Abstractions;
using Customers.Application.DTOs;
using Customers.Domain.ValueObjects;
using Shared.Domain.Primitives;

namespace Customers.Application.UseCases.GetVehiclesByClient;

internal sealed class GetVehiclesByClientHandler : IQueryHandler<GetVehiclesByClientQuery, IReadOnlyList<VehicleDto>>
{
    private readonly IClientRepository _repository;

    public GetVehiclesByClientHandler(IClientRepository repository) => _repository = repository;

    public async Task<Result<IReadOnlyList<VehicleDto>>> Handle(GetVehiclesByClientQuery query, CancellationToken ct)
    {
        var client = await _repository.GetByIdAsync(ClientId.From(query.ClientId), ct);
        if (client is null)
            return Result.Failure<IReadOnlyList<VehicleDto>>(Error.NotFound("Client.NotFound", "Cliente não encontrado."));

        IReadOnlyList<VehicleDto> dtos = client.Vehicles.Select(VehicleDto.FromAggregate).ToList();
        return Result.Success(dtos);
    }
}
