using Identity.Domain.Aggregates;

namespace Identity.Application.Abstractions;

public interface IUserRepository
{
    Task AddAsync(User user, CancellationToken ct = default);
    Task<bool> ExistsByNormalizedEmailAsync(string normalizedEmail, CancellationToken ct = default);
    Task<User?> GetByNormalizedLoginAsync(string normalizedLogin, CancellationToken ct = default);
    Task<(User User, RefreshToken Token)?> GetByRefreshTokenHashAsync(string tokenHash, CancellationToken ct = default);

    /// <summary>Marca um refresh token novo como Added explicitamente (evita o EF tratá-lo como
    /// UPDATE de linha inexistente quando a coleção do User não está carregada).</summary>
    void AddRefreshToken(RefreshToken token);

    Task SaveChangesAsync(CancellationToken ct = default);
}
