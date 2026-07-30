using Identity.Application.Abstractions;
using Identity.Infrastructure.Persistence;
using Identity.Infrastructure.Persistence.Repositories;
using Identity.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("GearFlow")
            ?? throw new InvalidOperationException("ConnectionStrings:GearFlow não configurada.");

        services.AddDbContext<IdentityDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddSingleton<IPasswordHasher, IdentityPasswordHasher>();
        services.AddScoped<ITokenService, JwtTokenService>();

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(Application.Abstractions.ICommand).Assembly));

        return services;
    }
}
