using Customers.Application.Abstractions;
using Customers.Application.DTOs;
using Customers.Domain.ValueObjects;
using Shared.Domain.Primitives;

namespace Customers.Application.UseCases.GetVehicleById;

internal sealed class GetVehicleByIdHandler : IQueryHandler<GetVehicleByIdQuery, VehicleDto>
{
    private readonly IClientRepository _repository;

    public GetVehicleByIdHandler(IClientRepository repository) => _repository = repository;

    public async Task<Result<VehicleDto>> Handle(GetVehicleByIdQuery query, CancellationToken ct)
    {
        var vehicle = await _repository.GetVehicleByIdAsync(VehicleId.From(query.VehicleId), ct);
        if (vehicle is null)
            return Result.Failure<VehicleDto>(Error.NotFound("Vehicle.NotFound", "Veículo não encontrado."));

        return Result.Success(VehicleDto.FromAggregate(vehicle));
    }
}
