using System.ComponentModel.DataAnnotations;

namespace Shared.Infrastructure.Auth;

/// <summary>
/// Config tipada do JWT (seção "Jwt"). Validada no boot via ValidateOnStart em
/// <see cref="JwtAuthenticationExtensions.AddJwtAuthentication"/> — se o secret faltar ou for
/// curto demais, o processo falha ao subir com mensagem clara, em vez de estourar em runtime.
/// Fonte única para o validador (JwtBearer), o gerador de token de staff (Identity BC) e a Lambda
/// de CPF (que assina com o MESMO secret/issuer/audience).
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>Chave HMAC-SHA256. Mínimo 32 bytes (256 bits) — exigência do algoritmo.</summary>
    [Required(AllowEmptyStrings = false)]
    [MinLength(32, ErrorMessage = "Jwt:Secret deve ter no mínimo 32 caracteres (HMAC-SHA256).")]
    public string Secret { get; init; } = string.Empty;

    public string Issuer { get; init; } = "gearflow";

    public string Audience { get; init; } = "gearflow";

    /// <summary>
    /// Nullable de propósito: o config binder faz <c>GetValue&lt;int&gt;</c> incondicional para value
    /// types e retornaria 0 quando a chave está ausente. Com <c>int?</c>, ausente = null → o
    /// <see cref="Range"/> só valida quando informado; o default de 24h aplica em <see cref="ExpiresInSeconds"/>.
    /// </summary>
    [Range(1, 24 * 365, ErrorMessage = "Jwt:ExpiresInHours deve estar entre 1 e 8760 (1 ano).")]
    public int? ExpiresInHours { get; init; }

    public int ExpiresInSeconds => (ExpiresInHours ?? 24) * 3600;
}
