using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Shared.Domain.Security;

namespace Shared.Infrastructure.Security;

/// <summary>
/// Resolve o <see cref="Actor"/> a partir dos claims do JWT do request. O token de cliente (Lambda
/// de CPF) carrega <c>customer_id</c>; o token de staff carrega o id do usuário em
/// <c>NameIdentifier</c>/<c>sub</c>. Sem HttpContext (worker, event handler, startup) a ação é do
/// sistema.
/// </summary>
internal sealed class HttpCurrentActor : ICurrentActor
{
    private readonly IHttpContextAccessor _accessor;

    public HttpCurrentActor(IHttpContextAccessor accessor) => _accessor = accessor;

    public Actor Current
    {
        get
        {
            if (_accessor.HttpContext?.User is not { } user)
                return Actor.System;

            if (TryClaim(user, "customer_id", out var customerId)) return Actor.Customer(customerId);

            if (Guid.TryParse(
                    user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value,
                    out var userId))
                return Actor.Staff(userId);

            return Actor.System;
        }
    }

    private static bool TryClaim(ClaimsPrincipal user, string claimType, out Guid value) =>
        Guid.TryParse(user.FindFirst(claimType)?.Value, out value);
}
