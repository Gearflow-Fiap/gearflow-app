namespace Workshop.Application.Abstractions;

public sealed record CustomerContact(string Name, string Email);

/// <summary>
/// Lê o contato do cliente dono de um veículo (para o e-mail do orçamento). Implementada na
/// Infrastructure via SQL cru contra customers.vehicles/customers.clients (ADR-002).
/// </summary>
public interface ICustomerContactReader
{
    Task<CustomerContact?> GetByVehicleAsync(Guid vehicleId, CancellationToken ct = default);
}
