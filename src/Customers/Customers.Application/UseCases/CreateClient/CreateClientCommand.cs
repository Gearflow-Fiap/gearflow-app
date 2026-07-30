using Customers.Application.Abstractions;
using Customers.Application.DTOs;

namespace Customers.Application.UseCases.CreateClient;

public sealed record CreateClientCommand(
    string? Cpf,
    string? Cnpj,
    string Name,
    string Email,
    string Phone,
    AddressDto Address) : ICommand<ClientDto>;
