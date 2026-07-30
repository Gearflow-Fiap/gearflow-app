using Customers.Application.Abstractions;
using Customers.Application.DTOs;

namespace Customers.Application.UseCases.GetClients;

public sealed record GetClientsQuery : IQuery<IReadOnlyList<ClientDto>>;
