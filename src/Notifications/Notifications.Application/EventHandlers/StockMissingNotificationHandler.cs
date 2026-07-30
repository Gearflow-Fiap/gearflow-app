using MediatR;
using Notifications.Application.Abstractions;
using Notifications.Domain.Aggregates;
using Shared.Contracts.IntegrationEvents.Workshop;

namespace Notifications.Application.EventHandlers;

/// <summary>Falta de estoque na aprovação → aviso ao estoquista. Preserva o StockMissingEmailHandler do legado.</summary>
internal sealed class StockMissingNotificationHandler : INotificationHandler<StockMissingIntegrationEvent>
{
    private readonly INotificationRepository _repository;

    public StockMissingNotificationHandler(INotificationRepository repository) => _repository = repository;

    public async Task Handle(StockMissingIntegrationEvent e, CancellationToken ct)
    {
        var subject = $"Reposição necessária — OS {e.ServiceOrderId}";
        var message = $"Orçamento {e.BudgetId} aprovado, porém há estoque insuficiente. {e.Detail}";

        var notification = new Notification(
            NotificationType.Part, NotificationChannel.Application, subject, message,
            "sistema@gearflow.com", "estoque@gearflow.com", e.OccurredOn);

        await _repository.AddAsync(notification, ct);
        await _repository.SaveChangesAsync(ct);
    }
}
