namespace Identity.Application.DTOs;

public sealed record AuthTokenResponse(string AccessToken, string RefreshToken, DateTime AccessTokenExpiresAtUtc);

public sealed record RegisterUserResponse(Guid UserId, string Email, string UserName);
