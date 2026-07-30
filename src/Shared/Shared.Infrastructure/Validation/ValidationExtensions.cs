using FluentValidation;
using Shared.Domain.Primitives;

namespace Shared.Infrastructure.Validation;

public static class ValidationExtensions
{
    public static async Task<Result<T>> ValidateAsync<T>(
        this IValidator<T> validator,
        T request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);

        if (validationResult.IsValid)
            return Result.Success(request);

        var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
        return Result.Failure<T>(new Error("Validation.Failed", errors));
    }
}
