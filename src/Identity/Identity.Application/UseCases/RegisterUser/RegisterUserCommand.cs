using Identity.Application.Abstractions;
using Identity.Application.DTOs;

namespace Identity.Application.UseCases.RegisterUser;

public sealed record RegisterUserCommand(string Email, string UserName, string Password) : ICommand<RegisterUserResponse>;
