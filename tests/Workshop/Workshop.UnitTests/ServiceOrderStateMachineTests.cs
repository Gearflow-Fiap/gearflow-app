using FluentAssertions;
using Shared.Domain.Security;
using Workshop.Domain.Aggregates.ServiceOrderModel;
using Workshop.Domain.Enums;

namespace Workshop.UnitTests;

public sealed class ServiceOrderStateMachineTests
{
    private static readonly Actor Staff = Actor.Staff(Guid.NewGuid());
    private static readonly DateTime Now = new(2026, 7, 30, 12, 0, 0, DateTimeKind.Utc);

    private static ServiceOrder NewOrder() =>
        ServiceOrder.Create(Guid.NewGuid(), Staff, [new ServiceOrderJob(Guid.NewGuid(), Now)], null, Now);

    [Fact]
    public void New_order_starts_received_with_history()
    {
        var order = NewOrder();

        order.Status.Should().Be(ServiceOrderStatus.Received);
        order.Histories.Should().ContainSingle();
        order.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Happy_path_walks_full_lifecycle()
    {
        var order = NewOrder();

        order.StartDiagnostic(Staff, Now).IsSuccess.Should().BeTrue();
        order.FinalizeDiagnostic(Staff, Now).IsSuccess.Should().BeTrue();
        order.StartExecution(Staff, Now).IsSuccess.Should().BeTrue();
        order.Finalize(Staff, Now).IsSuccess.Should().BeTrue();
        order.Deliver(Staff, Now).IsSuccess.Should().BeTrue();

        order.Status.Should().Be(ServiceOrderStatus.Delivered);
        order.Histories.Should().HaveCount(6); // received + 5 transições
    }

    [Fact]
    public void StartExecution_requires_awaiting_approval()
    {
        var order = NewOrder(); // Received

        var result = order.StartExecution(Staff, Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ServiceOrder.NotAwaitingApproval");
        order.Status.Should().Be(ServiceOrderStatus.Received);
    }

    [Fact]
    public void Insufficient_stock_path_goes_to_awaiting_parts_then_resumes()
    {
        var order = NewOrder();
        order.StartDiagnostic(Staff, Now);
        order.FinalizeDiagnostic(Staff, Now); // AwaitingApproval

        order.WaitingPartsOrConsumables(Staff, Now).IsSuccess.Should().BeTrue();
        order.Status.Should().Be(ServiceOrderStatus.AwaitingPartsOrConsumables);

        order.ResumeExecution(Staff, Now).IsSuccess.Should().BeTrue();
        order.Status.Should().Be(ServiceOrderStatus.InExecution);
    }

    [Fact]
    public void Cancel_only_from_awaiting_approval()
    {
        var order = NewOrder();
        order.Cancel(Staff, Now).IsFailure.Should().BeTrue();

        order.StartDiagnostic(Staff, Now);
        order.FinalizeDiagnostic(Staff, Now);
        order.Cancel(Staff, Now).IsSuccess.Should().BeTrue();
        order.Status.Should().Be(ServiceOrderStatus.Canceled);
    }

    [Fact]
    public void Update_vehicle_blocked_after_approval_stage()
    {
        var order = NewOrder();
        order.StartDiagnostic(Staff, Now);
        order.FinalizeDiagnostic(Staff, Now); // AwaitingApproval

        var result = order.Update(Guid.NewGuid(), Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ServiceOrder.VehicleLocked");
    }

    [Fact]
    public void Deactivate_only_before_approval()
    {
        var order = NewOrder();
        order.Deactivate(Staff, Now).IsSuccess.Should().BeTrue();
        order.IsActive.Should().BeFalse();
    }
}
