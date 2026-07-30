using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Notifications.Application.Abstractions;
using Notifications.Application.EventHandlers;
using Notifications.Domain.Aggregates;
using NSubstitute;
using Shared.Contracts.IntegrationEvents.Inventory;
using Shared.Contracts.IntegrationEvents.Workshop;

namespace Notifications.UnitTests;

public sealed class NotificationHandlerTests
{
    private static readonly DateTime Now = DateTime.UtcNow;
    private readonly INotificationRepository _repo = Substitute.For<INotificationRepository>();
    private readonly IEmailSender _email = Substitute.For<IEmailSender>();

    private static IConfiguration Config() =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["PublicBaseUrl"] = "http://gearflow.local"
        }).Build();

    [Fact]
    public async Task BudgetGenerated_emails_client_and_records_notification()
    {
        var handler = new BudgetGeneratedEmailHandler(_repo, _email, Config(), NullLogger<BudgetGeneratedEmailHandler>.Instance);
        var e = new BudgetGeneratedIntegrationEvent(Guid.NewGuid(), Now, Guid.NewGuid(), Guid.NewGuid(), "João", "joao@x.com", 15000);

        await handler.Handle(e, CancellationToken.None);

        await _email.Received(1).SendAsync("joao@x.com", Arg.Any<string>(), Arg.Is<string>(b => b.Contains("/approve")), Arg.Any<CancellationToken>());
        await _repo.Received(1).AddAsync(Arg.Is<Notification>(n => n.Type == NotificationType.Budget), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BudgetGenerated_without_email_skips_send_but_records()
    {
        var handler = new BudgetGeneratedEmailHandler(_repo, _email, Config(), NullLogger<BudgetGeneratedEmailHandler>.Instance);
        var e = new BudgetGeneratedIntegrationEvent(Guid.NewGuid(), Now, Guid.NewGuid(), Guid.NewGuid(), "João", "", 15000);

        await handler.Handle(e, CancellationToken.None);

        await _email.DidNotReceive().SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _repo.Received(1).AddAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BudgetApproved_records_internal_notification()
    {
        var handler = new BudgetApprovedNotificationHandler(_repo);
        await handler.Handle(new BudgetApprovedIntegrationEvent(Guid.NewGuid(), Now, Guid.NewGuid(), Guid.NewGuid(), StockReserved: true), CancellationToken.None);

        await _repo.Received(1).AddAsync(Arg.Is<Notification>(n => n.To == "oficina@gearflow.com"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StockMissing_records_stock_notification()
    {
        var handler = new StockMissingNotificationHandler(_repo);
        await handler.Handle(new StockMissingIntegrationEvent(Guid.NewGuid(), Now, Guid.NewGuid(), Guid.NewGuid(), "sem estoque"), CancellationToken.None);

        await _repo.Received(1).AddAsync(Arg.Is<Notification>(n => n.To == "estoque@gearflow.com"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LowStockAlert_records_and_tries_email()
    {
        var handler = new LowStockAlertHandler(_repo, _email, NullLogger<LowStockAlertHandler>.Instance);
        await handler.Handle(new LowStockAlertIntegrationEvent(Guid.NewGuid(), Now, "Part", Guid.NewGuid(), "Filtro", 3, 5), CancellationToken.None);

        await _repo.Received(1).AddAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
        await _email.Received(1).SendAsync("estoque@gearflow.com", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
