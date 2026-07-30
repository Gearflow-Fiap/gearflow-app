namespace Notifications.Application.Abstractions;

/// <summary>Envio de e-mail (SMTP/Mailpit em dev). Falha é logada, não quebra o fluxo de negócio.</summary>
public interface IEmailSender
{
    Task SendAsync(string to, string subject, string body, CancellationToken ct = default);
}
