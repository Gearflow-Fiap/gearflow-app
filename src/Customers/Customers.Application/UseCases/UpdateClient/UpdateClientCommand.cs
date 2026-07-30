using Customers.Application.Abstractions;
using Customers.Application.DTOs;

namespace Customers.Application.UseCases.UpdateClient;

public sealed record UpdateClientCommand(
    Guid ClientId, string Name, string Email, string Phone, AddressDto Address) : ICommand<ClientDto>;
