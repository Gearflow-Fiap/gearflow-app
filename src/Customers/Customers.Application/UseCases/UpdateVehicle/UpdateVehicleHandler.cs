using Customers.Application.Abstractions;
using Customers.Application.DTOs;
using Customers.Domain.ValueObjects;
using Shared.Domain.Primitives;

namespace Customers.Application.UseCases.UpdateVehicle;

internal sealed class UpdateVehicleHandler : ICommandHandler<UpdateVehicleCommand, VehicleDto>
{
    private readonly IClientRepository _repository;

    public UpdateVehicleHandler(IClientRepository repository) => _repository = repository;

    public async Task<Result<VehicleDto>> Handle(UpdateVehicleCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.LicensePlate))
            return Result.Failure<VehicleDto>(Error.Validation("Vehicle.LicensePlateRequired", "Placa é obrigatória.", "licensePlate"));

        var vehicle = await _repository.GetVehicleByIdAsync(VehicleId.From(command.VehicleId), ct);
        if (vehicle is null)
            return Result.Failure<VehicleDto>(Error.NotFound("Vehicle.NotFound", "Veículo não encontrado."));

        vehicle.UpdateDetails(command.LicensePlate, command.Mark, command.Model, command.Color,
            command.YearFabrication, command.YearModel);

        await _repository.SaveChangesAsync(ct);
        return Result.Success(VehicleDto.FromAggregate(vehicle));
    }
}
