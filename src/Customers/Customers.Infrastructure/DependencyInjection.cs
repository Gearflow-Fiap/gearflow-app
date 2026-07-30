using Customers.Application.Abstractions;
using Customers.Infrastructure.Persistence;
using Customers.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Customers.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCustomersInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("GearFlow")
            ?? throw new InvalidOperationException("ConnectionStrings:GearFlow não configurada.");

        services.AddDbContext<CustomersDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IClientRepository, ClientRepository>();

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(Application.Abstractions.ICommand).Assembly));

        return services;
    }
}
