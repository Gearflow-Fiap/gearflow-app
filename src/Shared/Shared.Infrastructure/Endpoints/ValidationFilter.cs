using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Shared.Infrastructure.Endpoints;

/// <summary>
/// Valida o request DTO <typeparamref name="TRequest"/> via o <see cref="IValidator{T}"/> registrado
/// (FluentValidation) antes do handler; falha → ValidationProblem (RFC 9457) com erros por campo.
/// Sem validator registrado, passa direto. Aplicar via
/// <see cref="EndpointExtensions.WithValidation{TRequest}"/>.
/// </summary>
public sealed class ValidationFilter<TRequest> : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var validator = context.HttpContext.RequestServices.GetService<IValidator<TRequest>>();
        if (validator is not null && context.Arguments.OfType<TRequest>().FirstOrDefault() is { } request)
        {
            var validation = await validator.ValidateAsync(request, context.HttpContext.RequestAborted);
            if (!validation.IsValid)
            {
                return TypedResults.ValidationProblem(
                    validation.ToDictionary(),
                    extensions: new Dictionary<string, object?> { ["code"] = "Validation.Failed" });
            }
        }

        return await next(context);
    }
}
