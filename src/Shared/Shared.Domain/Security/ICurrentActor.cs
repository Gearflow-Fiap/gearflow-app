namespace Shared.Domain.Security;

/// <summary>
/// Resolve quem originou o request corrente. Handlers de Application dependem desta abstração
/// para registrar autoria sem conhecer HTTP — a implementação (que lê os claims do JWT) vive na
/// Infrastructure, como manda a direção de dependência. Fora de um request HTTP — workers, event
/// handlers, startup — devolve <see cref="Actor.System"/>.
/// </summary>
public interface ICurrentActor
{
    Actor Current { get; }
}
