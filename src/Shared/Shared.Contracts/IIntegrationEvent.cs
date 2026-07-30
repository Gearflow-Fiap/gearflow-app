using MediatR;

namespace Shared.Contracts;

/// <summary>
/// Marca um contrato de evento de integração entre BCs. É um <see cref="INotification"/> do MediatR
/// para dispatch in-process no monólito modular; se um BC for extraído para um serviço próprio, o
/// mesmo contrato passa a trafegar por um broker sem mudar o publisher/subscriber.
/// </summary>
public interface IIntegrationEvent : INotification
{
    Guid EventId { get; }
    DateTime OccurredOn { get; }
}
