using Identity.Application.Abstractions;
using Microsoft.AspNetCore.Identity;

namespace Identity.Infrastructure.Security;

/// <summary>Preserva o esquema do GearFlow: <see cref="PasswordHasher{T}"/> do ASP.NET Core Identity.</summary>
internal sealed class IdentityPasswordHasher : IPasswordHasher
{
    private static readonly PasswordHasher<object> Hasher = new();
    private static readonly object Dummy = new();

    public string Hash(string password) => Hasher.HashPassword(Dummy, password);

    public bool Verify(string passwordHash, string providedPassword)
    {
        if (string.IsNullOrEmpty(passwordHash)) return false;
        var result = Hasher.VerifyHashedPassword(Dummy, passwordHash, providedPassword);
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
