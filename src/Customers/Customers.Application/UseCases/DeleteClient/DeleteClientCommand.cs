using Customers.Application.Abstractions;

namespace Customers.Application.UseCases.DeleteClient;

public sealed record DeleteClientCommand(Guid ClientId) : ICommand;
