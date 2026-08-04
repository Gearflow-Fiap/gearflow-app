using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Shared.Infrastructure.Middleware;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // wiring do pipeline — fora da meta de cobertura significativa
public static class MiddlewareExtensions
{
    /// <summary>
    /// Registra o ProblemDetails (RFC 9457) como shape único de erro da API. Injeta traceId/
    /// correlationId em toda resposta de problema. Chamar no Program.cs ao lado de AddObservability.
    /// Pareado com <see cref="UseExceptionHandling"/> (erros não-tratados → 500 no mesmo shape) e
    /// com ResultExtensions (erros de negócio via Result → ProblemDetails).
    /// </summary>
    public static IServiceCollection AddSharedErrorHandling(this IServiceCollection services)
    {
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = ctx =>
            {
                var traceId = System.Diagnostics.Activity.Current?.TraceId.ToString()
                    ?? (ctx.HttpContext.Items.TryGetValue("TraceId", out var cid)
                        ? cid?.ToString()
                        : ctx.HttpContext.TraceIdentifier);
                ctx.ProblemDetails.Extensions["traceId"] = traceId;
                ctx.HttpContext.Response.Headers["X-Trace-Id"] = traceId;
            };
        });
        return services;
    }

    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}
