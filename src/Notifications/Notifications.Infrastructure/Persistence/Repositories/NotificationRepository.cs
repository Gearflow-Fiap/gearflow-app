using Notifications.Application.Abstractions;
using Notifications.Domain.Aggregates;
using Notifications.Infrastructure.Persistence;

namespace Notifications.Infrastructure.Persistence.Repositories;

internal sealed class NotificationRepository : INotificationRepository
{
    private readonly NotificationsDbContext _db;

    public NotificationRepository(NotificationsDbContext db) => _db = db;

    public async Task AddAsync(Notification notification, CancellationToken ct = default) =>
        await _db.Notifications.AddAsync(notification, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
