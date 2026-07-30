using FluentAssertions;
using MediatR;
using NSubstitute;
using Shared.Contracts.IntegrationEvents.Workshop;
using Shared.Domain.Security;
using Workshop.Application.Abstractions;
using Workshop.Application.UseCases.ApproveBudget;
using Workshop.Application.UseCases.ExecuteJob;
using Workshop.Application.UseCases.FinalizeDiagnostic;
using Workshop.Application.UseCases.GetServiceOrders;
using Workshop.Application.EventHandlers;
using Workshop.Application.UseCases.ResumeExecution;
using Shared.Contracts.IntegrationEvents.Inventory;
using Workshop.Domain.Aggregates.BudgetModel;
using Workshop.Domain.Aggregates.ServiceOrderModel;
using Workshop.Domain.Enums;

namespace Workshop.UnitTests.Handlers;

public sealed class ServiceOrderHandlerTests
{
    private static readonly DateTime Now = DateTime.UtcNow;
    private readonly IServiceOrderRepository _orders = Substitute.For<IServiceOrderRepository>();
    private readonly IBudgetRepository _budgets = Substitute.For<IBudgetRepository>();
    private readonly IInventoryReservation _inventory = Substitute.For<IInventoryReservation>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();
    private readonly ICurrentActor _actor = Substitute.For<ICurrentActor>();

    public ServiceOrderHandlerTests() => _actor.Current.Returns(Actor.System);

    private static (ServiceOrder order, Budget budget) AtAwaitingApproval(Guid jobId, Guid partId, int qty)
    {
        var order = ServiceOrder.Create(Guid.NewGuid(), Actor.System,
            [new ServiceOrderJob(jobId, Now)], [new ServiceOrderPart(partId, qty, Now)], Now);
        order.StartDiagnostic(Actor.System, Now);
        order.FinalizeDiagnostic(Actor.System, Now);
        var budget = Budget.Create(order.Id,
            [new BudgetJob(jobId, 10000, Now)], [new BudgetPart(partId, 5000, qty, Now)], [], Now);
        return (order, budget);
    }

    [Fact]
    public async Task ApproveBudget_with_stock_moves_to_execution_and_publishes_approved()
    {
        var (order, budget) = AtAwaitingApproval(Guid.NewGuid(), Guid.NewGuid(), 2);
        _orders.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        _budgets.GetByServiceOrderAsync(order.Id, Arg.Any<CancellationToken>()).Returns(budget);
        _inventory.ReserveAsync(Arg.Any<IReadOnlyList<ReservationItem>>(), Arg.Any<CancellationToken>())
            .Returns(new ReservationResult(ReservationStatus.Ok));

        var handler = new ApproveBudgetHandler(_orders, _budgets, _inventory, _publisher, _actor, TimeProvider.System);
        var result = await handler.Handle(new ApproveBudgetCommand(order.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(ServiceOrderStatus.InExecution);
        await _publisher.Received(1).Publish(
            Arg.Is<BudgetApprovedIntegrationEvent>(e => e.StockReserved), Arg.Any<CancellationToken>());
        await _publisher.DidNotReceive().Publish(Arg.Any<StockMissingIntegrationEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApproveBudget_without_stock_waits_and_publishes_stock_missing()
    {
        var (order, budget) = AtAwaitingApproval(Guid.NewGuid(), Guid.NewGuid(), 5);
        _orders.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        _budgets.GetByServiceOrderAsync(order.Id, Arg.Any<CancellationToken>()).Returns(budget);
        _inventory.ReserveAsync(Arg.Any<IReadOnlyList<ReservationItem>>(), Arg.Any<CancellationToken>())
            .Returns(new ReservationResult(ReservationStatus.Insufficient, "sem estoque"));

        var handler = new ApproveBudgetHandler(_orders, _budgets, _inventory, _publisher, _actor, TimeProvider.System);
        var result = await handler.Handle(new ApproveBudgetCommand(order.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(ServiceOrderStatus.AwaitingPartsOrConsumables);
        await _publisher.Received(1).Publish(
            Arg.Is<BudgetApprovedIntegrationEvent>(e => !e.StockReserved), Arg.Any<CancellationToken>());
        await _publisher.Received(1).Publish(Arg.Any<StockMissingIntegrationEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteJob_fails_when_order_not_in_execution()
    {
        var (order, budget) = AtAwaitingApproval(Guid.NewGuid(), Guid.NewGuid(), 1); // AwaitingApproval
        _orders.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        _budgets.GetByServiceOrderAsync(order.Id, Arg.Any<CancellationToken>()).Returns(budget);

        var handler = new ExecuteJobHandler(_orders, _budgets, TimeProvider.System);
        var result = await handler.Handle(new ExecuteJobCommand(order.Id.Value, budget.Jobs.First().Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ServiceOrder.NotInExecution");
    }

    [Fact]
    public async Task ResumeExecution_advances_only_when_reservation_succeeds()
    {
        var (order, budget) = AtAwaitingApproval(Guid.NewGuid(), Guid.NewGuid(), 2);
        order.WaitingPartsOrConsumables(Actor.System, Now); // AwaitingPartsOrConsumables
        _orders.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        _budgets.GetByServiceOrderAsync(order.Id, Arg.Any<CancellationToken>()).Returns(budget);
        _inventory.ReserveAsync(Arg.Any<IReadOnlyList<ReservationItem>>(), Arg.Any<CancellationToken>())
            .Returns(new ReservationResult(ReservationStatus.Ok));

        var handler = new ResumeExecutionHandler(_orders, _budgets, _inventory, _actor, TimeProvider.System);
        var result = await handler.Handle(new ResumeExecutionCommand(order.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(ServiceOrderStatus.InExecution);
    }

    [Fact]
    public async Task ResumeExecution_stays_waiting_when_stock_still_insufficient()
    {
        var (order, budget) = AtAwaitingApproval(Guid.NewGuid(), Guid.NewGuid(), 2);
        order.WaitingPartsOrConsumables(Actor.System, Now);
        _orders.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        _budgets.GetByServiceOrderAsync(order.Id, Arg.Any<CancellationToken>()).Returns(budget);
        _inventory.ReserveAsync(Arg.Any<IReadOnlyList<ReservationItem>>(), Arg.Any<CancellationToken>())
            .Returns(new ReservationResult(ReservationStatus.Insufficient, "ainda sem estoque"));

        var handler = new ResumeExecutionHandler(_orders, _budgets, _inventory, _actor, TimeProvider.System);
        var result = await handler.Handle(new ResumeExecutionCommand(order.Id.Value), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        order.Status.Should().Be(ServiceOrderStatus.AwaitingPartsOrConsumables);
    }

    [Fact]
    public async Task FinalizeDiagnostic_publishes_budget_generated_with_client_contact()
    {
        var jobId = Guid.NewGuid();
        var order = ServiceOrder.Create(Guid.NewGuid(), Actor.System, [new ServiceOrderJob(jobId, Now)], null, Now);
        order.StartDiagnostic(Actor.System, Now);
        _orders.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var pricing = Substitute.For<IPricingReader>();
        pricing.GetJobPricesAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, int> { [jobId] = 12000 });
        pricing.GetPartPricesAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, int>());
        pricing.GetConsumablePricesAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, int>());

        var contacts = Substitute.For<ICustomerContactReader>();
        contacts.GetByVehicleAsync(order.VehicleId, Arg.Any<CancellationToken>())
            .Returns(new CustomerContact("João", "joao@x.com"));

        var handler = new FinalizeDiagnosticHandler(_orders, _budgets, pricing, contacts, _publisher, _actor, TimeProvider.System);
        var result = await handler.Handle(
            new FinalizeDiagnosticCommand(order.Id.Value, Array.Empty<DiagnosticConsumableInput>()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(ServiceOrderStatus.AwaitingApproval);
        await _budgets.Received(1).AddAsync(Arg.Any<Budget>(), Arg.Any<CancellationToken>());
        await _publisher.Received(1).Publish(
            Arg.Is<BudgetGeneratedIntegrationEvent>(e => e.ClientEmail == "joao@x.com"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PartsReplenished_resumes_eligible_awaiting_order()
    {
        var partId = Guid.NewGuid();
        var (order, budget) = AtAwaitingApproval(Guid.NewGuid(), partId, 2);
        order.WaitingPartsOrConsumables(Actor.System, Now); // AwaitingPartsOrConsumables
        _orders.GetAwaitingPartsAsync(Arg.Any<CancellationToken>()).Returns(new List<ServiceOrder> { order });
        _budgets.GetByServiceOrderAsync(order.Id, Arg.Any<CancellationToken>()).Returns(budget);
        _inventory.ReserveAsync(Arg.Any<IReadOnlyList<ReservationItem>>(), Arg.Any<CancellationToken>())
            .Returns(new ReservationResult(ReservationStatus.Ok));

        var handler = new PartsReplenishedHandler(_orders, _budgets, _inventory, TimeProvider.System);
        await handler.Handle(new PartsReplenishedIntegrationEvent(Guid.NewGuid(), Now, "Part", partId), CancellationToken.None);

        order.Status.Should().Be(ServiceOrderStatus.InExecution);
        await _orders.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PartsReplenished_ignores_orders_not_referencing_the_item()
    {
        var (order, budget) = AtAwaitingApproval(Guid.NewGuid(), Guid.NewGuid(), 2);
        order.WaitingPartsOrConsumables(Actor.System, Now);
        _orders.GetAwaitingPartsAsync(Arg.Any<CancellationToken>()).Returns(new List<ServiceOrder> { order });
        _budgets.GetByServiceOrderAsync(order.Id, Arg.Any<CancellationToken>()).Returns(budget);

        var handler = new PartsReplenishedHandler(_orders, _budgets, _inventory, TimeProvider.System);
        await handler.Handle(new PartsReplenishedIntegrationEvent(Guid.NewGuid(), Now, "Part", Guid.NewGuid()), CancellationToken.None);

        order.Status.Should().Be(ServiceOrderStatus.AwaitingPartsOrConsumables);
        await _inventory.DidNotReceive().ReserveAsync(Arg.Any<IReadOnlyList<ReservationItem>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetServiceOrders_clamps_page_and_pagesize()
    {
        _orders.GetPagedByPriorityAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((new List<ServiceOrder>(), 0));

        var handler = new GetServiceOrdersHandler(_orders);
        await handler.Handle(new GetServiceOrdersQuery(Page: 0, PageSize: 1000), CancellationToken.None);

        await _orders.Received(1).GetPagedByPriorityAsync(1, 100, Arg.Any<CancellationToken>());
    }
}
