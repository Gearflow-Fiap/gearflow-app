using Notifications.Domain.Aggregates;

namespace Notifications.Application.Abstractions;

public interface INotificationRepository
{
    Task AddAsync(Notification notification, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
