using Workshop.Application.Abstractions;

namespace Workshop.Application.UseCases.FinalizeServiceOrder;

public sealed record FinalizeServiceOrderCommand(Guid ServiceOrderId) : ICommand;
