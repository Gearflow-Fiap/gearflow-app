using Workshop.Application.Abstractions;

namespace Workshop.Application.UseCases.DeactivateServiceOrder;

public sealed record DeactivateServiceOrderCommand(Guid ServiceOrderId) : ICommand;
