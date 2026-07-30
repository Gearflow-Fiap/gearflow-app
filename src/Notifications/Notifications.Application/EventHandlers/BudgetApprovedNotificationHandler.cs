using MediatR;
using Notifications.Application.Abstractions;
using Notifications.Domain.Aggregates;
using Shared.Contracts.IntegrationEvents.Workshop;

namespace Notifications.Application.EventHandlers;

/// <summary>Orçamento aprovado → aviso interno à oficina. Preserva o BudgetApprovedEmailHandler do legado.</summary>
internal sealed class BudgetApprovedNotificationHandler : INotificationHandler<BudgetApprovedIntegrationEvent>
{
    private readonly INotificationRepository _repository;

    public BudgetApprovedNotificationHandler(INotificationRepository repository) => _repository = repository;

    public async Task Handle(BudgetApprovedIntegrationEvent e, CancellationToken ct)
    {
        var subject = $"Orçamento aprovado — OS {e.ServiceOrderId}";
        var message = e.StockReserved
            ? "Estoque reservado. Execução iniciada."
            : "Aguardando reposição de estoque para iniciar a execução.";

        var notification = new Notification(
            NotificationType.Budget, NotificationChannel.Application, subject, message,
            "sistema@gearflow.com", "oficina@gearflow.com", e.OccurredOn);

        await _repository.AddAsync(notification, ct);
        await _repository.SaveChangesAsync(ct);
    }
}
