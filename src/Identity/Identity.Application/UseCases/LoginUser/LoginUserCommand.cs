using Identity.Application.Abstractions;
using Identity.Application.DTOs;

namespace Identity.Application.UseCases.LoginUser;

public sealed record LoginUserCommand(string EmailOrUserName, string Password, string? IpAddress) : ICommand<AuthTokenResponse>;
