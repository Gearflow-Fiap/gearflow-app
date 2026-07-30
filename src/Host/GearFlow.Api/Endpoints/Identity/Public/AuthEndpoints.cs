using Identity.Application.DTOs;
using Identity.Application.UseCases.LoginUser;
using Identity.Application.UseCases.RefreshAccessToken;
using Identity.Application.UseCases.RegisterUser;
using Identity.Application.UseCases.RevokeToken;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Shared.Infrastructure.Endpoints;
using Shared.Infrastructure.Http;

namespace GearFlow.Api.Endpoints.Identity.Public;

/// <summary>Autenticação de staff (anônimo). Login por e-mail/usuário + senha, refresh de token.</summary>
public sealed class AuthEndpoints : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/identity/auth").WithTags("Identity");

        group.MapPost("/register", async (RegisterUserCommand command, ISender sender, CancellationToken ct) =>
                (await sender.Send(command, ct)).ToOk())
            .WithSummary("Registra um funcionário (staff).")
            .WithDescription("Cria um usuário de staff (e-mail, nome e senha). Valida senha mínima e e-mail único.")
            .Produces<RegisterUserResponse>()
            .ProducesValidationProblem();

        group.MapPost("/login", async (LoginRequest body, HttpContext http, ISender sender, CancellationToken ct) =>
                (await sender.Send(new LoginUserCommand(body.EmailOrUserName, body.Password, ClientIp(http)), ct)).ToOk())
            .WithSummary("Login por e-mail/usuário + senha.")
            .WithDescription("Retorna access token (JWT) + refresh token. Respeita bloqueio por tentativas.")
            .Produces<AuthTokenResponse>()
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/refresh-token", async (RefreshRequest body, HttpContext http, ISender sender, CancellationToken ct) =>
                (await sender.Send(new RefreshAccessTokenCommand(body.RefreshToken, ClientIp(http)), ct)).ToOk())
            .WithSummary("Rotaciona o refresh token e emite um novo par.")
            .WithDescription("Recebe um refresh token válido, revoga-o e devolve um novo access token + refresh token.")
            .Produces<AuthTokenResponse>()
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/revoke-token", async (RefreshRequest body, HttpContext http, ISender sender, CancellationToken ct) =>
                (await sender.Send(new RevokeTokenCommand(body.RefreshToken, ClientIp(http)), ct)).ToOk())
            .WithSummary("Revoga um refresh token (logout).")
            .WithDescription("Invalida o refresh token informado para encerrar a sessão; 404 se já estiver inválido/revogado.")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static string? ClientIp(HttpContext http) => http.Connection.RemoteIpAddress?.ToString();

    public sealed record LoginRequest(string EmailOrUserName, string Password);
    public sealed record RefreshRequest(string RefreshToken);
}
