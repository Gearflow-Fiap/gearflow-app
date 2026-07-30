namespace Shared.Infrastructure.Http;

/// <summary>
/// Contrato público único para respostas HTTP de erro. Os campos de RFC 9457 descrevem o
/// problema; <see cref="Code"/> é o discriminador estável para clientes, <see cref="TraceId"/>
/// correlaciona a resposta com logs/traces e <see cref="Errors"/> preserva validações por campo.
/// </summary>
public sealed record ApiProblemDetails
{
    public string? Type { get; init; }
    public required string Title { get; init; }
    public required int Status { get; init; }
    public required string Detail { get; init; }
    public string? Instance { get; init; }
    public required string Code { get; init; }
    public required string TraceId { get; init; }
    public IReadOnlyDictionary<string, string[]>? Errors { get; init; }
}
