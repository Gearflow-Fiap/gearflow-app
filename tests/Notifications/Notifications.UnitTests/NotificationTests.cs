using FluentAssertions;
using Notifications.Domain.Aggregates;

namespace Notifications.UnitTests;

public sealed class NotificationTests
{
    [Fact]
    public void Ctor_populates_all_fields_and_generates_id()
    {
        var sentOn = new DateTime(2026, 8, 4, 12, 0, 0, DateTimeKind.Utc);

        var notification = new Notification(
            NotificationType.Budget, NotificationChannel.Email,
            "Orçamento gerado", "Seu orçamento está pronto.",
            "system@gearflow.com", "cliente@x.com", sentOn, "{\"budgetId\":\"1\"}");

        notification.Id.Should().NotBe(Guid.Empty);
        notification.Type.Should().Be(NotificationType.Budget);
        notification.Channel.Should().Be(NotificationChannel.Email);
        notification.Title.Should().Be("Orçamento gerado");
        notification.Message.Should().Be("Seu orçamento está pronto.");
        notification.From.Should().Be("system@gearflow.com");
        notification.To.Should().Be("cliente@x.com");
        notification.DataJson.Should().Be("{\"budgetId\":\"1\"}");
        notification.SentOn.Should().Be(sentOn);
    }

    [Fact]
    public void Ctor_allows_null_data_json()
    {
        var notification = new Notification(
            NotificationType.Part, NotificationChannel.Application,
            "Estoque baixo", "Item X em falta.", "system@gearflow.com", "estoque@gearflow.com",
            DateTime.UtcNow);

        notification.DataJson.Should().BeNull();
    }
}
