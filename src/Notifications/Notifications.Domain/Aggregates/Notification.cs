using Shared.Domain.Primitives;

namespace Notifications.Domain.Aggregates;

public enum NotificationType { Part = 1, Consumable = 2, Budget = 3, ServiceOrder = 4, Client = 5 }

public enum NotificationChannel { Application = 1, Email = 2 }

/// <summary>Registro de uma notificação enviada (preservado do GearFlow).</summary>
public sealed class Notification : AggregateRoot<Guid>
{
    public NotificationType Type { get; private set; }
    public NotificationChannel Channel { get; private set; }
    public string Title { get; private set; }
    public string Message { get; private set; }
    public string From { get; private set; }
    public string To { get; private set; }
    public string? DataJson { get; private set; }
    public DateTime SentOn { get; private set; }

    public Notification(
        NotificationType type, NotificationChannel channel, string title, string message,
        string from, string to, DateTime sentOn, string? dataJson = null) : base(Guid.NewGuid())
    {
        Type = type;
        Channel = channel;
        Title = title;
        Message = message;
        From = from;
        To = to;
        DataJson = dataJson;
        SentOn = sentOn;
    }

    private Notification() : base(Guid.NewGuid())
    {
        Title = null!;
        Message = null!;
        From = null!;
        To = null!;
    }
}
