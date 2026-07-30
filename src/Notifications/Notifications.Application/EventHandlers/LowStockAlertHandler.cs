using MediatR;
using Microsoft.Extensions.Logging;
using Notifications.Application.Abstractions;
using Notifications.Domain.Aggregates;
using Shared.Contracts.IntegrationEvents.Inventory;

namespace Notifications.Application.EventHandlers;

/// <summary>
/// Reage ao alerta de estoque mínimo (evento in-process): registra a notificação e avisa o estoquista
/// por e-mail. Preserva o <c>LowStockAlertHandler</c> do GearFlow como assinante cross-BC.
/// </summary>
internal sealed class LowStockAlertHandler : INotificationHandler<LowStockAlertIntegrationEvent>
{
    private const string StockEmail = "estoque@gearflow.com";

    private readonly INotificationRepository _repository;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<LowStockAlertHandler> _logger;

    public LowStockAlertHandler(
        INotificationRepository repository, IEmailSender emailSender, ILogger<LowStockAlertHandler> logger)
    {
        _repository = repository;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task Handle(LowStockAlertIntegrationEvent e, CancellationToken ct)
    {
        var title = $"Estoque baixo: {e.ItemName}";
        var message = $"O item '{e.ItemName}' ({e.ItemType}) está em {e.RemainingQuantity} (mínimo {e.MinimumQuantity}).";

        var notification = new Notification(
            e.ItemType == "Part" ? NotificationType.Part : NotificationType.Consumable,
            NotificationChannel.Email, title, message, "system@gearflow.com", StockEmail, e.OccurredOn);

        await _repository.AddAsync(notification, ct);
        await _repository.SaveChangesAsync(ct);

        try
        {
            await _emailSender.SendAsync(StockEmail, title, message, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao enviar e-mail de estoque baixo para {To}", StockEmail);
        }
    }
}
