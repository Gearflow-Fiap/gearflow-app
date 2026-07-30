using Microsoft.Extensions.DependencyInjection;
using Shared.Domain.Security;

namespace Shared.Infrastructure.Security;

public static class CurrentActorExtensions
{
    /// <summary>
    /// Registra o resolvedor de <see cref="Actor"/> a partir do JWT do request. Scoped porque o
    /// ator é por request. Fora de HTTP devolve <see cref="Actor.System"/>.
    /// </summary>
    public static IServiceCollection AddCurrentActor(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentActor, HttpCurrentActor>();
        return services;
    }
}
