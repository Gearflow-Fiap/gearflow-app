using Shared.Contracts;
using Shared.Domain.Primitives;
using Shared.Domain.Security;
using Workshop.Application.Abstractions;
using Workshop.Domain.Enums;
using Workshop.Domain.ValueObjects;

namespace Workshop.Application.UseCases.DeliverServiceOrder;

internal sealed class DeliverServiceOrderHandler : ICommandHandler<DeliverServiceOrderCommand>
{
    private readonly IServiceOrderRepository _repository;
    private readonly ICurrentActor _currentActor;
    private readonly TimeProvider _timeProvider;
    private readonly IBusinessMetrics _metrics;

    public DeliverServiceOrderHandler(
        IServiceOrderRepository repository, ICurrentActor currentActor, TimeProvider timeProvider, IBusinessMetrics metrics)
    {
        _repository = repository;
        _currentActor = currentActor;
        _timeProvider = timeProvider;
        _metrics = metrics;
    }

    public async Task<Result> Handle(DeliverServiceOrderCommand command, CancellationToken ct)
    {
        var order = await _repository.GetByIdAsync(ServiceOrderId.From(command.ServiceOrderId), ct);
        if (order is null)
            return Result.Failure(Error.NotFound("ServiceOrder.NotFound", "Ordem de serviço não encontrada."));

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var finalizedStart = order.Histories
            .Where(h => h.Status == ServiceOrderStatus.Finalized)
            .OrderBy(h => h.CreatedOn).FirstOrDefault();

        var result = order.Deliver(_currentActor.Current, now);
        if (result.IsFailure) return result;

        if (finalizedStart is not null)
            _metrics.ServiceOrderStatusDuration("Finalizacao", (now - finalizedStart.CreatedOn).TotalMinutes);

        await _repository.SaveChangesAsync(ct);
        return Result.Success();
    }
}
