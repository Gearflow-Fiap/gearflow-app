using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notifications.Application.Abstractions;
using Notifications.Infrastructure.Email;
using Notifications.Infrastructure.Persistence;
using Notifications.Infrastructure.Persistence.Repositories;

namespace Notifications.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationsInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("GearFlow")
            ?? throw new InvalidOperationException("ConnectionStrings:GearFlow não configurada.");

        services.AddDbContext<NotificationsDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();

        // Registra os event handlers (INotificationHandler) do BC para o dispatch in-process do MediatR.
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(Application.Abstractions.INotificationRepository).Assembly));

        return services;
    }
}
