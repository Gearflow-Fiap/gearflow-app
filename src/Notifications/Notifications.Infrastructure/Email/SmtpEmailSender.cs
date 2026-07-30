using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Notifications.Application.Abstractions;

namespace Notifications.Infrastructure.Email;

/// <summary>
/// Envio via SMTP (Mailpit em dev). Config em <c>Smtp:Host</c>/<c>Smtp:Port</c>/<c>Smtp:From</c>.
/// Preserva o <c>SmtpEmailSender</c> do GearFlow.
/// </summary>
internal sealed class SmtpEmailSender : IEmailSender
{
    private readonly string _host;
    private readonly int _port;
    private readonly string _from;

    public SmtpEmailSender(IConfiguration configuration)
    {
        _host = configuration["Smtp:Host"] ?? "localhost";
        _port = int.TryParse(configuration["Smtp:Port"], out var p) ? p : 1025;
        _from = configuration["Smtp:From"] ?? "no-reply@gearflow.com";
    }

    public async Task SendAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        using var client = new SmtpClient(_host, _port);
        using var message = new MailMessage(_from, to, subject, body);
        await client.SendMailAsync(message, ct);
    }
}
