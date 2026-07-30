using Identity.Application.Abstractions;
using Identity.Application.DTOs;

namespace Identity.Application.UseCases.RefreshAccessToken;

public sealed record RefreshAccessTokenCommand(string RefreshToken, string? IpAddress) : ICommand<AuthTokenResponse>;
