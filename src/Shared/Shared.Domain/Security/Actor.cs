using Shared.Domain.Primitives;

namespace Shared.Domain.Security;

/// <summary>Quem originou uma ação. Ver <see cref="Actor"/>.</summary>
public enum ActorType
{
    /// <summary>Processo automático — worker, event handler, seed, startup. Não há usuário por trás.</summary>
    System = 0,

    /// <summary>Funcionário da oficina autenticado (JWT staff, login por e-mail/usuário). O <c>Id</c> é o <c>UserId</c>.</summary>
    Staff = 1,

    /// <summary>Cliente da oficina autenticado pela Lambda de CPF (Fase 3). O <c>Id</c> é o <c>ClientId</c>.</summary>
    Customer = 2,
}

/// <summary>
/// Quem originou uma ação — o "quem" que a auditoria e a timeline da OS registram. Vive em
/// Shared.Domain por ser cross-BC (regra: utilitário cross-BC mora em Shared.Domain/Shared.Contracts).
/// É um VO puro — quem resolve o ator do request é <see cref="ICurrentActor"/>, implementado na
/// Infrastructure a partir dos claims do JWT.
/// </summary>
public sealed class Actor : ValueObject
{
    public ActorType Type { get; }

    /// <summary>Id do ator. Nulo para <see cref="ActorType.System"/>, que não tem identidade.</summary>
    public Guid? Id { get; }

    private Actor(ActorType type, Guid? id)
    {
        Type = type;
        Id = id;
    }

    public static readonly Actor System = new(ActorType.System, null);

    public static Actor Staff(Guid userId) => new(ActorType.Staff, userId);
    public static Actor Customer(Guid clientId) => new(ActorType.Customer, clientId);

    /// <summary>Reidrata do banco (duas colunas: tipo + id).</summary>
    public static Actor From(ActorType type, Guid? id) => new(type, id);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Type;
        yield return Id ?? Guid.Empty;
    }

    public override string ToString() => Id is null ? Type.ToString() : $"{Type}:{Id}";
}
