using Customers.Application.Abstractions;
using Customers.Application.DTOs;
using Customers.Domain.ValueObjects;
using Shared.Domain.Primitives;
using Shared.Domain.Security;

namespace Customers.Application.UseCases.AddVehicle;

internal sealed class AddVehicleHandler : ICommandHandler<AddVehicleCommand, VehicleDto>
{
    private readonly IClientRepository _repository;
    private readonly ICurrentActor _currentActor;
    private readonly TimeProvider _timeProvider;

    public AddVehicleHandler(IClientRepository repository, ICurrentActor currentActor, TimeProvider timeProvider)
    {
        _repository = repository;
        _currentActor = currentActor;
        _timeProvider = timeProvider;
    }

    public async Task<Result<VehicleDto>> Handle(AddVehicleCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.LicensePlate))
            return Result.Failure<VehicleDto>(Error.Validation("Vehicle.LicensePlateRequired", "Placa é obrigatória.", "licensePlate"));

        var client = await _repository.GetByIdAsync(ClientId.From(command.ClientId), ct);
        if (client is null)
            return Result.Failure<VehicleDto>(Error.NotFound("Client.NotFound", "Cliente não encontrado."));

        var createdById = _currentActor.Current.Id ?? Guid.Empty;
        var vehicle = client.AddVehicle(
            command.LicensePlate, command.Mark, command.Model, command.Color,
            command.YearFabrication, command.YearModel, createdById, _timeProvider.GetUtcNow().UtcDateTime);

        _repository.AddVehicle(vehicle);   // garante estado Added (evita UPDATE de linha inexistente)
        await _repository.SaveChangesAsync(ct);

        return Result.Success(VehicleDto.FromAggregate(vehicle));
    }
}
