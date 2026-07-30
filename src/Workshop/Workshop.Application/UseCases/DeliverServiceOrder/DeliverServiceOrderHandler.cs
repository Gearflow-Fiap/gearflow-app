using Shared.Domain.Primitives;
using Shared.Domain.Security;
using Workshop.Application.Abstractions;
using Workshop.Domain.ValueObjects;

namespace Workshop.Application.UseCases.DeliverServiceOrder;

internal sealed class DeliverServiceOrderHandler : ICommandHandler<DeliverServiceOrderCommand>
{
    private readonly IServiceOrderRepository _repository;
    private readonly ICurrentActor _currentActor;
    private readonly TimeProvider _timeProvider;

    public DeliverServiceOrderHandler(IServiceOrderRepository repository, ICurrentActor currentActor, TimeProvider timeProvider)
    {
        _repository = repository;
        _currentActor = currentActor;
        _timeProvider = timeProvider;
    }

    public async Task<Result> Handle(DeliverServiceOrderCommand command, CancellationToken ct)
    {
        var order = await _repository.GetByIdAsync(ServiceOrderId.From(command.ServiceOrderId), ct);
        if (order is null)
            return Result.Failure(Error.NotFound("ServiceOrder.NotFound", "Ordem de serviço não encontrada."));

        var result = order.Deliver(_currentActor.Current, _timeProvider.GetUtcNow().UtcDateTime);
        if (result.IsFailure) return result;

        await _repository.SaveChangesAsync(ct);
        return Result.Success();
    }
}
