using Customers.Domain.Aggregates;
using Customers.Domain.ValueObjects;

namespace Customers.Application.Abstractions;

public interface IClientRepository
{
    Task AddAsync(Client client, CancellationToken ct = default);
    Task<Client?> GetByIdAsync(ClientId id, CancellationToken ct = default);
    Task<IReadOnlyList<Client>> ListAsync(CancellationToken ct = default);
    void Remove(Client client);

    Task<Vehicle?> GetVehicleByIdAsync(VehicleId id, CancellationToken ct = default);
    void AddVehicle(Vehicle vehicle);
    void RemoveVehicle(Vehicle vehicle);

    Task SaveChangesAsync(CancellationToken ct = default);
}
