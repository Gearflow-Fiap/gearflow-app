using Shared.Domain.Primitives;
using Shared.Domain.Security;
using Workshop.Application.Abstractions;
using Workshop.Application.DTOs;
using Workshop.Domain.Aggregates.ServiceOrderModel;

namespace Workshop.Application.UseCases.CreateServiceOrder;

internal sealed class CreateServiceOrderHandler : ICommandHandler<CreateServiceOrderCommand, ServiceOrderDto>
{
    private readonly IServiceOrderRepository _repository;
    private readonly ICurrentActor _currentActor;
    private readonly TimeProvider _timeProvider;

    public CreateServiceOrderHandler(
        IServiceOrderRepository repository, ICurrentActor currentActor, TimeProvider timeProvider)
    {
        _repository = repository;
        _currentActor = currentActor;
        _timeProvider = timeProvider;
    }

    public async Task<Result<ServiceOrderDto>> Handle(CreateServiceOrderCommand command, CancellationToken ct)
    {
        if (command.JobIds.Count == 0)
            return Result.Failure<ServiceOrderDto>(
                Error.Validation("ServiceOrder.NoJobs", "Informe ao menos um serviço para a OS."));

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var jobs = command.JobIds.Select(id => new ServiceOrderJob(id, now));
        var parts = command.Parts.Select(p => new ServiceOrderPart(p.PartId, p.Quantity, now));

        var order = ServiceOrder.Create(command.VehicleId, _currentActor.Current, jobs, parts, now);

        await _repository.AddAsync(order, ct);
        await _repository.SaveChangesAsync(ct);

        return Result.Success(ServiceOrderDto.FromAggregate(order));
    }
}
