using Identity.Application.Abstractions;
using Identity.Application.DTOs;
using Identity.Domain.Aggregates;
using Shared.Domain.Primitives;

namespace Identity.Application.UseCases.RegisterUser;

internal sealed class RegisterUserHandler : ICommandHandler<RegisterUserCommand, RegisterUserResponse>
{
    private readonly IUserRepository _repository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly TimeProvider _timeProvider;

    public RegisterUserHandler(IUserRepository repository, IPasswordHasher passwordHasher, TimeProvider timeProvider)
    {
        _repository = repository;
        _passwordHasher = passwordHasher;
        _timeProvider = timeProvider;
    }

    public async Task<Result<RegisterUserResponse>> Handle(RegisterUserCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Password) || command.Password.Length < 6)
            return Result.Failure<RegisterUserResponse>(
                Error.Validation("User.WeakPassword", "A senha deve ter ao menos 6 caracteres.", "password"));

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var normalizedEmail = User.Normalize(command.Email.Trim());
        if (await _repository.ExistsByNormalizedEmailAsync(normalizedEmail, ct))
            return Result.Failure<RegisterUserResponse>(Error.Conflict("User.EmailAlreadyExists", "E-mail já cadastrado."));

        var userResult = User.Create(command.Email, command.UserName, now);
        if (userResult.IsFailure)
            return Result.Failure<RegisterUserResponse>(userResult.Error);

        var user = userResult.Value;
        user.SetPasswordHash(_passwordHasher.Hash(command.Password), now);

        await _repository.AddAsync(user, ct);
        await _repository.SaveChangesAsync(ct);

        return Result.Success(new RegisterUserResponse(user.Id.Value, user.Email, user.UserName));
    }
}
