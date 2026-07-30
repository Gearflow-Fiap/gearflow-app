using MediatR;
using Shared.Contracts.IntegrationEvents.Workshop;
using Shared.Domain.Primitives;
using Shared.Domain.Security;
using Workshop.Application.Abstractions;
using Workshop.Domain.Aggregates.BudgetModel;
using Workshop.Domain.ValueObjects;

namespace Workshop.Application.UseCases.FinalizeDiagnostic;

/// <summary>
/// Move a OS de InDiagnostic → AwaitingApproval e cria o <c>Budget</c> com os preços atuais
/// (server-authoritative, lidos ao vivo via <see cref="IPricingReader"/>). Persistidos na mesma
/// transação.
/// </summary>
internal sealed class FinalizeDiagnosticHandler : ICommandHandler<FinalizeDiagnosticCommand>
{
    private readonly IServiceOrderRepository _orders;
    private readonly IBudgetRepository _budgets;
    private readonly IPricingReader _pricing;
    private readonly ICustomerContactReader _contacts;
    private readonly IPublisher _publisher;
    private readonly ICurrentActor _currentActor;
    private readonly TimeProvider _timeProvider;

    public FinalizeDiagnosticHandler(
        IServiceOrderRepository orders, IBudgetRepository budgets, IPricingReader pricing,
        ICustomerContactReader contacts, IPublisher publisher, ICurrentActor currentActor, TimeProvider timeProvider)
    {
        _orders = orders;
        _budgets = budgets;
        _pricing = pricing;
        _contacts = contacts;
        _publisher = publisher;
        _currentActor = currentActor;
        _timeProvider = timeProvider;
    }

    public async Task<Result> Handle(FinalizeDiagnosticCommand command, CancellationToken ct)
    {
        var order = await _orders.GetByIdAsync(ServiceOrderId.From(command.ServiceOrderId), ct);
        if (order is null)
            return Result.Failure(Error.NotFound("ServiceOrder.NotFound", "Ordem de serviço não encontrada."));

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var transition = order.FinalizeDiagnostic(_currentActor.Current, now);
        if (transition.IsFailure) return transition;

        var jobIds = order.RequestedJobs.Select(j => j.JobId).ToList();
        var partIds = order.RequestedParts.Select(p => p.PartId).ToList();
        var consumableIds = command.Consumables.Select(c => c.ConsumableId).ToList();

        var jobPrices = await _pricing.GetJobPricesAsync(jobIds, ct);
        var partPrices = await _pricing.GetPartPricesAsync(partIds, ct);
        var consumablePrices = await _pricing.GetConsumablePricesAsync(consumableIds, ct);

        var budgetJobs = order.RequestedJobs
            .Select(j => new BudgetJob(j.JobId, jobPrices.GetValueOrDefault(j.JobId), now)).ToList();
        var budgetParts = order.RequestedParts
            .Select(p => new BudgetPart(p.PartId, partPrices.GetValueOrDefault(p.PartId), p.Quantity, now)).ToList();
        var budgetConsumables = command.Consumables
            .Select(c => new BudgetConsumable(c.ConsumableId, consumablePrices.GetValueOrDefault(c.ConsumableId), c.Quantity, now)).ToList();

        var budget = Budget.Create(order.Id, budgetJobs, budgetParts, budgetConsumables, now);

        await _budgets.AddAsync(budget, ct);
        await _orders.SaveChangesAsync(ct); // mesmo DbContext do Workshop → uma transação

        // Notifica o cliente (e-mail com links de aprovar/rejeitar) — preserva o BudgetGeneratedEvent.
        var contact = await _contacts.GetByVehicleAsync(order.VehicleId, ct);
        await _publisher.Publish(
            new BudgetGeneratedIntegrationEvent(
                Guid.NewGuid(), now, budget.Id.Value, order.Id.Value,
                contact?.Name ?? string.Empty, contact?.Email ?? string.Empty, budget.TotalPriceCents),
            ct);

        return Result.Success();
    }
}
