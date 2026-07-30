using Workshop.Application.Abstractions;

namespace Workshop.Application.UseCases.DeliverServiceOrder;

public sealed record DeliverServiceOrderCommand(Guid ServiceOrderId) : ICommand;
