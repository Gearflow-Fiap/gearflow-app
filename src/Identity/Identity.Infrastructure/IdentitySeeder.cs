using Identity.Application.Abstractions;
using Identity.Domain.Aggregates;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.Infrastructure;

/// <summary>
/// Semeia um usuário de staff padrão (paridade com o AdminSeeder legado), para permitir login no demo.
/// Idempotente: só cria se não houver nenhum usuário. Roda em Development, ou em outros ambientes se
/// <c>Seed:EnableDevData</c> for <c>true</c>.
/// </summary>
public static class IdentitySeeder
{
    public const string DefaultEmail = "admin@gearflow.local";
    public const string DefaultUserName = "admin";
    public const string DefaultPassword = "Admin@123";

    public static async Task SeedAsync(IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<IdentityDbContext>();

        if (await db.Users.AnyAsync(ct))
            return;

        var hasher = sp.GetRequiredService<IPasswordHasher>();
        var now = sp.GetRequiredService<TimeProvider>().GetUtcNow().UtcDateTime;

        var userResult = User.Create(DefaultEmail, DefaultUserName, now);
        if (userResult.IsFailure)
            return;

        var user = userResult.Value;
        user.SetPasswordHash(hasher.Hash(DefaultPassword), now);

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
    }
}
