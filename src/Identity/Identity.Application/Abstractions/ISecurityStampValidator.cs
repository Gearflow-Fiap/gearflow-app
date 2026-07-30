namespace Identity.Application.Abstractions;

/// <summary>
/// Verifica se o <c>security_stamp</c> apresentado num JWT de staff ainda é o vigente para o usuário
/// (e se ele continua ativo). Preserva a revogação server-side do GearFlow: trocar a senha rotaciona
/// o stamp e invalida todos os JWTs em circulação.
/// </summary>
public interface ISecurityStampValidator
{
    Task<bool> IsCurrentAsync(Guid userId, string securityStamp, CancellationToken ct = default);
}
