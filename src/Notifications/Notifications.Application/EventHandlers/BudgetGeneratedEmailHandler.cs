using System.Globalization;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Notifications.Application.Abstractions;
using Notifications.Domain.Aggregates;
using Shared.Contracts.IntegrationEvents.Workshop;

namespace Notifications.Application.EventHandlers;

/// <summary>
/// Orçamento gerado → e-mail ao cliente com os links de aprovar/rejeitar + registro da notificação.
/// Preserva o BudgetGeneratedEmailHandler do legado.
/// </summary>
internal sealed class BudgetGeneratedEmailHandler : INotificationHandler<BudgetGeneratedIntegrationEvent>
{
    private static readonly CultureInfo PtBr = new("pt-BR");

    private readonly INotificationRepository _repository;
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BudgetGeneratedEmailHandler> _logger;

    public BudgetGeneratedEmailHandler(
        INotificationRepository repository, IEmailSender emailSender,
        IConfiguration configuration, ILogger<BudgetGeneratedEmailHandler> logger)
    {
        _repository = repository;
        _emailSender = emailSender;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task Handle(BudgetGeneratedIntegrationEvent e, CancellationToken ct)
    {
        var baseUrl = (_configuration["PublicBaseUrl"] ?? "http://localhost:5000").TrimEnd('/');
        var approveLink = $"{baseUrl}/api/workshop/budgets/{e.BudgetId}/approve";
        var rejectLink = $"{baseUrl}/api/workshop/budgets/{e.BudgetId}/reject";
        var total = (e.TotalPriceCents / 100m).ToString("N2", PtBr);

        var subject = $"Orçamento para aprovação — OS {e.ServiceOrderId}";
        var body =
            $"Olá {e.ClientName},\n\nO orçamento da sua ordem de serviço está pronto. Valor total: R$ {total}.\n\n" +
            $"Aprovar: {approveLink}\nRejeitar: {rejectLink}\n";

        if (!string.IsNullOrWhiteSpace(e.ClientEmail))
        {
            try
            {
                await _emailSender.SendAsync(e.ClientEmail, subject, body, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao enviar e-mail de orçamento para {To} (Budget {BudgetId}).",
                    e.ClientEmail, e.BudgetId);
            }
        }

        var notification = new Notification(
            NotificationType.Budget, NotificationChannel.Email, subject,
            $"{subject} — Cliente: {e.ClientName} <{e.ClientEmail}>. Valor: R$ {total}. Aprovar: {approveLink}",
            "sistema@gearflow.com", e.ClientEmail, e.OccurredOn);

        await _repository.AddAsync(notification, ct);
        await _repository.SaveChangesAsync(ct);
    }
}
