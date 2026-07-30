using Shared.Domain.Primitives;
using Workshop.Application.Abstractions;
using Workshop.Domain.ValueObjects;

namespace Workshop.Application.UseCases.UpdateServiceOrderVehicle;

/// <summary>Troca o veículo da OS. Bloqueado após a aprovação do orçamento (regra do agregado).</summary>
internal sealed class UpdateServiceOrderVehicleHandler : ICommandHandler<UpdateServiceOrderVehicleCommand>
{
    private readonly IServiceOrderRepository _repository;
    private readonly TimeProvider _timeProvider;

    public UpdateServiceOrderVehicleHandler(IServiceOrderRepository repository, TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public async Task<Result> Handle(UpdateServiceOrderVehicleCommand command, CancellationToken ct)
    {
        var order = await _repository.GetByIdAsync(ServiceOrderId.From(command.ServiceOrderId), ct);
        if (order is null)
            return Result.Failure(Error.NotFound("ServiceOrder.NotFound", "Ordem de serviço não encontrada."));

        var result = order.Update(command.VehicleId, _timeProvider.GetUtcNow().UtcDateTime);
        if (result.IsFailure) return result;

        await _repository.SaveChangesAsync(ct);
        return Result.Success();
    }
}
