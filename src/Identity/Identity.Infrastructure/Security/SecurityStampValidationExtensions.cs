using System.Security.Claims;
using Identity.Application.Abstractions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.Infrastructure.Security;

public static class SecurityStampValidationExtensions
{
    /// <summary>
    /// Liga a revalidação do <c>security_stamp</c> no pipeline do JWT Bearer: em cada request com
    /// token de staff, confere se o stamp do token bate com o vigente no banco (e se o usuário está
    /// ativo). Tokens de cliente (Lambda de CPF) não têm <c>security_stamp</c> e são ignorados aqui.
    /// Chamar no host DEPOIS de AddJwtAuthentication + AddIdentityInfrastructure.
    /// </summary>
    public static IServiceCollection AddStaffSecurityStampValidation(this IServiceCollection services)
    {
        services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.Events ??= new JwtBearerEvents();
            options.Events.OnTokenValidated = async context =>
            {
                var principal = context.Principal;
                var stamp = principal?.FindFirst("security_stamp")?.Value;

                // Sem stamp = não é token de staff (ex.: cliente via CPF). Nada a revalidar.
                if (string.IsNullOrEmpty(stamp)) return;

                var idClaim = principal!.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? principal.FindFirst("sub")?.Value;

                if (!Guid.TryParse(idClaim, out var userId))
                {
                    context.Fail("Token sem subject válido.");
                    return;
                }

                var validator = context.HttpContext.RequestServices.GetRequiredService<ISecurityStampValidator>();
                if (!await validator.IsCurrentAsync(userId, stamp, context.HttpContext.RequestAborted))
                    context.Fail("Sessão expirada (credencial alterada).");
            };
        });

        return services;
    }
}
