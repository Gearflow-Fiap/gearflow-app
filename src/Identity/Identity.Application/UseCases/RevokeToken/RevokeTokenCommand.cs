using Identity.Application.Abstractions;

namespace Identity.Application.UseCases.RevokeToken;

public sealed record RevokeTokenCommand(string RefreshToken, string? IpAddress) : ICommand;
