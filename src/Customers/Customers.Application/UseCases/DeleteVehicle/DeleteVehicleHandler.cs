using Customers.Application.Abstractions;
using Customers.Domain.ValueObjects;
using Shared.Domain.Primitives;

namespace Customers.Application.UseCases.DeleteVehicle;

internal sealed class DeleteVehicleHandler : ICommandHandler<DeleteVehicleCommand>
{
    private readonly IClientRepository _repository;

    public DeleteVehicleHandler(IClientRepository repository) => _repository = repository;

    public async Task<Result> Handle(DeleteVehicleCommand command, CancellationToken ct)
    {
        var vehicle = await _repository.GetVehicleByIdAsync(VehicleId.From(command.VehicleId), ct);
        if (vehicle is null)
            return Result.Failure(Error.NotFound("Vehicle.NotFound", "Veículo não encontrado."));

        _repository.RemoveVehicle(vehicle);
        await _repository.SaveChangesAsync(ct);
        return Result.Success();
    }
}
