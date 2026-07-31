using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Workshop.Application.Abstractions;
using Workshop.Infrastructure.CrossBc;
using Workshop.Infrastructure.Persistence;
using Workshop.Infrastructure.Persistence.Repositories;

namespace Workshop.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddWorkshopInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("GearFlow")
            ?? throw new InvalidOperationException("ConnectionStrings:GearFlow não configurada.");

        services.AddDbContext<WorkshopDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IServiceOrderRepository, ServiceOrderRepository>();
        services.AddScoped<IBudgetRepository, BudgetRepository>();

        // Portas cross-BC (ADR-002): SQL cru contra catalog.jobs / inventory.* / customers.*.
        services.AddScoped<IPricingReader>(_ => new SqlPricingReader(connectionString));
        services.AddScoped<IInventoryReservation>(_ => new SqlInventoryReservation(connectionString));
        services.AddScoped<ICustomerContactReader>(_ => new SqlCustomerContactReader(connectionString));

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(Application.Abstractions.ICommand).Assembly));

        return services;
    }
}
