using Customers.Application.Abstractions;
using Customers.Application.DTOs;

namespace Customers.Application.UseCases.GetClientById;

public sealed record GetClientByIdQuery(Guid ClientId) : IQuery<ClientDto>;
