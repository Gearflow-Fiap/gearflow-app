using FluentAssertions;
using MediatR;
using NSubstitute;
using Shared.Contracts;
using Shared.Contracts.IntegrationEvents.Inventory;
using Shared.Contracts.IntegrationEvents.Workshop;
using Shared.Domain.Security;
using Workshop.Application.Abstractions;
using Workshop.Application.UseCases.ApproveBudgetById;
using Workshop.Application.UseCases.DeactivateServiceOrder;
using Workshop.Application.UseCases.DeliverServiceOrder;
using Workshop.Application.UseCases.FinalizeServiceOrder;
using Workshop.Application.UseCases.RejectBudget;
using Workshop.Application.UseCases.RejectBudgetById;
using Workshop.Application.UseCases.StartDiagnostic;
using Workshop.Application.UseCases.UpdateServiceOrderVehicle;
using Workshop.Domain.Aggregates.BudgetModel;
using Workshop.Domain.Aggregates.ServiceOrderModel;
using Workshop.Domain.Enums;
using Workshop.Domain.ValueObjects;

namespace Workshop.UnitTests.Handlers;

/// <summary>
/// Cobre os handlers de transição da OS que não têm caminho próprio no fluxo de integração:
/// diagnóstico, entrega, desativação (soft-delete), troca de veículo, rejeição de orçamento
/// (keyed por OS e por id) e a aprovação/finalização com consumo de estoque.
/// </summary>
public sealed class WorkshopTransitionHandlerTests
{
    private static readonly DateTime Now = DateTime.UtcNow;
    private readonly IServiceOrderRepository _orders = Substitute.For<IServiceOrderRepository>();
    private readonly IBudgetRepository _budgets = Substitute.For<IBudgetRepository>();
    private readonly IInventoryReservation _inventory = Substitute.For<IInventoryReservation>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();
    private readonly ICurrentActor _actor = Substitute.For<ICurrentActor>();

    public WorkshopTransitionHandlerTests() => _actor.Current.Returns(Actor.System);

    private static ServiceOrder NewReceived(Guid? vehicleId = null) =>
        ServiceOrder.Create(vehicleId ?? Guid.NewGuid(), Actor.System,
            [new ServiceOrderJob(Guid.NewGuid(), Now)], null, Now);

    private static (ServiceOrder order, Budget budget) AtAwaitingApproval(Guid partId, int qty = 1)
    {
        var jobId = Guid.NewGuid();
        var order = ServiceOrder.Create(Guid.NewGuid(), Actor.System,
            [new ServiceOrderJob(jobId, Now)], [new ServiceOrderPart(partId, qty, Now)], Now);
        order.StartDiagnostic(Actor.System, Now);
        order.FinalizeDiagnostic(Actor.System, Now);
        var budget = Budget.Create(order.Id,
            [new BudgetJob(jobId, 10000, Now)], [new BudgetPart(partId, 5000, qty, Now)], [], Now);
        return (order, budget);
    }

    private static (ServiceOrder order, Budget budget) AtInExecution(Guid partId, int qty = 1)
    {
        var (order, budget) = AtAwaitingApproval(partId, qty);
        order.StartExecution(Actor.System, Now);
        return (order, budget);
    }

    // ---------- StartDiagnostic ----------

    [Fact]
    public async Task StartDiagnostic_moves_received_order_to_in_diagnostic()
    {
        var order = NewReceived();
        _orders.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var handler = new StartDiagnosticHandler(_orders, _actor, TimeProvider.System);
        var result = await handler.Handle(new StartDiagnosticCommand(order.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(ServiceOrderStatus.InDiagnostic);
        await _orders.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartDiagnostic_returns_notfound_when_order_absent()
    {
        _orders.GetByIdAsync(Arg.Any<ServiceOrderId>(), Arg.Any<CancellationToken>()).Returns((ServiceOrder?)null);

        var handler = new StartDiagnosticHandler(_orders, _actor, TimeProvider.System);
        var result = await handler.Handle(new StartDiagnosticCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ServiceOrder.NotFound");
    }

    // ---------- Deliver ----------

    [Fact]
    public async Task Deliver_moves_finalized_order_to_delivered()
    {
        var (order, _) = AtInExecution(Guid.NewGuid());
        order.Finalize(Actor.System, Now); // InExecution → Finalized
        _orders.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var handler = new DeliverServiceOrderHandler(_orders, _actor, TimeProvider.System);
        var result = await handler.Handle(new DeliverServiceOrderCommand(order.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(ServiceOrderStatus.Delivered);
    }

    [Fact]
    public async Task Deliver_fails_when_order_not_finalized()
    {
        var order = NewReceived();
        _orders.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var handler = new DeliverServiceOrderHandler(_orders, _actor, TimeProvider.System);
        var result = await handler.Handle(new DeliverServiceOrderCommand(order.Id.Value), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ServiceOrder.NotFinalized");
    }

    // ---------- Deactivate (soft-delete) ----------

    [Fact]
    public async Task Deactivate_soft_deletes_a_received_order()
    {
        var order = NewReceived();
        _orders.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var handler = new DeactivateServiceOrderHandler(_orders, _actor, TimeProvider.System);
        var result = await handler.Handle(new DeactivateServiceOrderCommand(order.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        order.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Deactivate_fails_after_approval()
    {
        var (order, _) = AtInExecution(Guid.NewGuid()); // já passou da aprovação
        _orders.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var handler = new DeactivateServiceOrderHandler(_orders, _actor, TimeProvider.System);
        var result = await handler.Handle(new DeactivateServiceOrderCommand(order.Id.Value), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ServiceOrder.CannotDeactivate");
        order.IsActive.Should().BeTrue();
    }

    // ---------- UpdateVehicle ----------

    [Fact]
    public async Task UpdateVehicle_changes_vehicle_before_approval()
    {
        var order = NewReceived();
        var newVehicle = Guid.NewGuid();
        _orders.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var handler = new UpdateServiceOrderVehicleHandler(_orders, TimeProvider.System);
        var result = await handler.Handle(new UpdateServiceOrderVehicleCommand(order.Id.Value, newVehicle), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        order.VehicleId.Should().Be(newVehicle);
    }

    [Fact]
    public async Task UpdateVehicle_is_locked_after_approval()
    {
        var (order, _) = AtAwaitingApproval(Guid.NewGuid()); // Status >= AwaitingApproval
        _orders.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var handler = new UpdateServiceOrderVehicleHandler(_orders, TimeProvider.System);
        var result = await handler.Handle(new UpdateServiceOrderVehicleCommand(order.Id.Value, Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ServiceOrder.VehicleLocked");
    }

    // ---------- RejectBudget (keyed por OS) ----------

    [Fact]
    public async Task RejectBudget_rejects_budget_and_cancels_order()
    {
        var (order, budget) = AtAwaitingApproval(Guid.NewGuid());
        _orders.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        _budgets.GetByServiceOrderAsync(order.Id, Arg.Any<CancellationToken>()).Returns(budget);

        var handler = new RejectBudgetHandler(_orders, _budgets, _actor, TimeProvider.System);
        var result = await handler.Handle(new RejectBudgetCommand(order.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        budget.IsApproved.Should().BeFalse();
        order.Status.Should().Be(ServiceOrderStatus.Canceled);
        await _orders.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RejectBudget_returns_notfound_when_budget_absent()
    {
        var order = NewReceived();
        _orders.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        _budgets.GetByServiceOrderAsync(order.Id, Arg.Any<CancellationToken>()).Returns((Budget?)null);

        var handler = new RejectBudgetHandler(_orders, _budgets, _actor, TimeProvider.System);
        var result = await handler.Handle(new RejectBudgetCommand(order.Id.Value), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Budget.NotFound");
    }

    // ---------- Approve/Reject por id do orçamento (fluxo do link de e-mail) ----------

    [Fact]
    public async Task ApproveBudgetById_reserves_and_moves_to_execution()
    {
        var partId = Guid.NewGuid();
        var (order, budget) = AtAwaitingApproval(partId, 2);
        _budgets.GetByIdAsync(Arg.Any<BudgetId>(), Arg.Any<CancellationToken>()).Returns(budget);
        _orders.GetByIdAsync(budget.ServiceOrderId, Arg.Any<CancellationToken>()).Returns(order);
        _inventory.ReserveAsync(Arg.Any<IReadOnlyList<ReservationItem>>(), Arg.Any<CancellationToken>())
            .Returns(new ReservationResult(ReservationStatus.Ok));

        var handler = new ApproveBudgetByIdHandler(_orders, _budgets, _inventory, _publisher, _actor, TimeProvider.System);
        var result = await handler.Handle(new ApproveBudgetByIdCommand(budget.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        budget.IsApproved.Should().BeTrue();
        order.Status.Should().Be(ServiceOrderStatus.InExecution);
        await _publisher.Received(1).Publish(
            Arg.Is<BudgetApprovedIntegrationEvent>(e => e.StockReserved), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApproveBudgetById_returns_notfound_when_budget_absent()
    {
        _budgets.GetByIdAsync(Arg.Any<BudgetId>(), Arg.Any<CancellationToken>()).Returns((Budget?)null);

        var handler = new ApproveBudgetByIdHandler(_orders, _budgets, _inventory, _publisher, _actor, TimeProvider.System);
        var result = await handler.Handle(new ApproveBudgetByIdCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Budget.NotFound");
    }

    [Fact]
    public async Task RejectBudgetById_rejects_and_cancels_order()
    {
        var (order, budget) = AtAwaitingApproval(Guid.NewGuid());
        _budgets.GetByIdAsync(Arg.Any<BudgetId>(), Arg.Any<CancellationToken>()).Returns(budget);
        _orders.GetByIdAsync(budget.ServiceOrderId, Arg.Any<CancellationToken>()).Returns(order);

        var handler = new RejectBudgetByIdHandler(_orders, _budgets, _actor, TimeProvider.System);
        var result = await handler.Handle(new RejectBudgetByIdCommand(budget.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        budget.IsApproved.Should().BeFalse();
        order.Status.Should().Be(ServiceOrderStatus.Canceled);
    }

    // ---------- FinalizeServiceOrder (consumo de estoque) ----------

    [Fact]
    public async Task Finalize_consumes_stock_and_moves_to_finalized()
    {
        var partId = Guid.NewGuid();
        var (order, budget) = AtInExecution(partId);
        _orders.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        _budgets.GetByServiceOrderAsync(order.Id, Arg.Any<CancellationToken>()).Returns(budget);
        _inventory.ConsumeAsync(Arg.Any<IReadOnlyList<ReservationItem>>(), Arg.Any<CancellationToken>())
            .Returns(new ReservationResult(ReservationStatus.Ok));

        var handler = new FinalizeServiceOrderHandler(_orders, _budgets, _inventory, _publisher, _actor, TimeProvider.System);
        var result = await handler.Handle(new FinalizeServiceOrderCommand(order.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(ServiceOrderStatus.Finalized);
        await _orders.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _publisher.DidNotReceive().Publish(Arg.Any<LowStockAlertIntegrationEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Finalize_publishes_low_stock_alert_when_consume_reports_low_stock()
    {
        var partId = Guid.NewGuid();
        var (order, budget) = AtInExecution(partId);
        _orders.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        _budgets.GetByServiceOrderAsync(order.Id, Arg.Any<CancellationToken>()).Returns(budget);
        _inventory.ConsumeAsync(Arg.Any<IReadOnlyList<ReservationItem>>(), Arg.Any<CancellationToken>())
            .Returns(new ReservationResult(ReservationStatus.Ok, null,
                new List<LowStockItem> { new(InventoryItemType.Part, partId, "Peça", 1, 5) }));

        var handler = new FinalizeServiceOrderHandler(_orders, _budgets, _inventory, _publisher, _actor, TimeProvider.System);
        var result = await handler.Handle(new FinalizeServiceOrderCommand(order.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _publisher.Received(1).Publish(Arg.Any<LowStockAlertIntegrationEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Finalize_fails_when_consume_reports_insufficient_stock()
    {
        var partId = Guid.NewGuid();
        var (order, budget) = AtInExecution(partId);
        _orders.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        _budgets.GetByServiceOrderAsync(order.Id, Arg.Any<CancellationToken>()).Returns(budget);
        _inventory.ConsumeAsync(Arg.Any<IReadOnlyList<ReservationItem>>(), Arg.Any<CancellationToken>())
            .Returns(new ReservationResult(ReservationStatus.Insufficient, "faltou"));

        var handler = new FinalizeServiceOrderHandler(_orders, _budgets, _inventory, _publisher, _actor, TimeProvider.System);
        var result = await handler.Handle(new FinalizeServiceOrderCommand(order.Id.Value), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Inventory.ConsumeFailed");
        await _orders.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Finalize_fails_when_order_not_in_execution()
    {
        var (order, budget) = AtAwaitingApproval(Guid.NewGuid()); // ainda em AwaitingApproval
        _orders.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        _budgets.GetByServiceOrderAsync(order.Id, Arg.Any<CancellationToken>()).Returns(budget);

        var handler = new FinalizeServiceOrderHandler(_orders, _budgets, _inventory, _publisher, _actor, TimeProvider.System);
        var result = await handler.Handle(new FinalizeServiceOrderCommand(order.Id.Value), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ServiceOrder.NotInExecution");
    }
}
