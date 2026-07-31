using FluentAssertions;
using Workshop.Domain.Aggregates.BudgetModel;
using Workshop.Domain.ValueObjects;

namespace Workshop.UnitTests;

/// <summary>
/// Agregado <see cref="Budget"/>: cálculo do total snapshotted e idempotência de aprovação/rejeição
/// (nasce com <c>IsApproved = null</c> = não revisado; revisar uma vez trava).
/// </summary>
public sealed class BudgetTests
{
    private static readonly DateTime Now = DateTime.UtcNow;

    private static Budget ABudget() => Budget.Create(
        ServiceOrderId.New(),
        [new BudgetJob(Guid.NewGuid(), 10000, Now), new BudgetJob(Guid.NewGuid(), 5000, Now)],
        [new BudgetPart(Guid.NewGuid(), 2000, 3, Now)],
        [new BudgetConsumable(Guid.NewGuid(), 800, 2.5m, Now)],
        Now);

    [Fact]
    public void Total_is_jobs_plus_parts_times_quantity_plus_consumables()
    {
        // 15000 (jobs) + 2000*3 (peça) + 800 (insumo) = 21800
        ABudget().TotalPriceCents.Should().Be(21800);
    }

    [Fact]
    public void Approve_sets_flag_and_timestamp_once()
    {
        var budget = ABudget();

        var first = budget.Approve(Now);

        first.IsSuccess.Should().BeTrue();
        budget.IsApproved.Should().BeTrue();
        budget.ApprovedOn.Should().Be(Now);
    }

    [Fact]
    public void Approve_twice_is_rejected_as_already_reviewed()
    {
        var budget = ABudget();
        budget.Approve(Now);

        var second = budget.Approve(Now.AddMinutes(1));

        second.IsFailure.Should().BeTrue();
        second.Error.Code.Should().Be("Budget.AlreadyReviewed");
    }

    [Fact]
    public void Reject_then_approve_is_rejected_as_already_reviewed()
    {
        var budget = ABudget();

        budget.Reject().IsSuccess.Should().BeTrue();
        budget.IsApproved.Should().BeFalse();

        budget.Approve(Now).Error.Code.Should().Be("Budget.AlreadyReviewed");
    }

    [Fact]
    public void MarkJobAsExecuted_marks_an_existing_job()
    {
        var budget = ABudget();
        var jobId = budget.Jobs.First().Id;

        var result = budget.MarkJobAsExecuted(jobId, Now);

        result.IsSuccess.Should().BeTrue();
        budget.Jobs.First(j => j.Id == jobId).IsExecuted.Should().BeTrue();
    }

    [Fact]
    public void MarkJobAsExecuted_unknown_job_returns_notfound()
    {
        ABudget().MarkJobAsExecuted(Guid.NewGuid(), Now).Error.Code.Should().Be("BudgetJob.NotFound");
    }

    [Fact]
    public void MarkJobAsExecuted_twice_returns_already_executed()
    {
        var budget = ABudget();
        var jobId = budget.Jobs.First().Id;
        budget.MarkJobAsExecuted(jobId, Now);

        budget.MarkJobAsExecuted(jobId, Now).Error.Code.Should().Be("BudgetJob.AlreadyExecuted");
    }
}
