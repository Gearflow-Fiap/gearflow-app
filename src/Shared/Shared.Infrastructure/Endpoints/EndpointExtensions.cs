using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Shared.Infrastructure.Endpoints;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // descoberta/registro de endpoints (wiring) — fora da meta
public static class EndpointExtensions
{
    /// <summary>Valida o request DTO via <see cref="ValidationFilter{TRequest}"/> antes do handler.</summary>
    public static RouteHandlerBuilder WithValidation<TRequest>(this RouteHandlerBuilder builder) =>
        builder.AddEndpointFilter<ValidationFilter<TRequest>>();

    /// <summary>
    /// Descobre todas as implementações de <see cref="IEndpoint"/> nos assemblies dados e as registra.
    /// Chamar no Program.cs: <c>app.MapEndpoints(typeof(Program).Assembly)</c>.
    /// </summary>
    public static WebApplication MapEndpoints(this WebApplication app, params Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            var endpointTypes = assembly.GetTypes()
                .Where(t => typeof(IEndpoint).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface);

            foreach (var type in endpointTypes)
            {
                var endpoint = (IEndpoint)Activator.CreateInstance(type)!;
                endpoint.MapEndpoints(app);
            }
        }

        return app;
    }

    /// <summary>Id do funcionário (staff) autenticado — claim padrão do JWT de staff (sub / NameIdentifier).</summary>
    public static Guid? GetUserId(this HttpContext context)
    {
        var claim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? context.User.FindFirst("sub")?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }

    /// <summary>
    /// Id do cliente autenticado pela Lambda de CPF (claim "customer_id"), se houver — endpoints
    /// públicos que aceitam cliente identificado usam isso sem exigir login de staff.
    /// </summary>
    public static Guid? GetCustomerId(this HttpContext context)
    {
        var claim = context.User.FindFirst("customer_id")?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}
