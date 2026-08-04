using FluentAssertions;
using Workshop.Domain.Aggregates.BudgetModel;
using Workshop.Domain.ValueObjects;

namespace Workshop.UnitTests;

public sealed class BudgetTests
{
    private static readonly DateTime Now = new(2026, 8, 4, 12, 0, 0, DateTimeKind.Utc);

    private static Budget ABudget(out BudgetJob job)
    {
        job = new BudgetJob(Guid.NewGuid(), 15000, Now);
        var parts = new List<BudgetPart> { new(Guid.NewGuid(), 5000, 2, Now) };       // 5000 * 2 = 10000
        var consumables = new List<BudgetConsumable> { new(Guid.NewGuid(), 3000, 1.5m, Now) }; // 3000
        return Budget.Create(ServiceOrderId.New(), new List<BudgetJob> { job }, parts, consumables, Now);
    }

    [Fact]
    public void Create_snapshots_total_price_from_items()
    {
        var budget = ABudget(out _);

        // job 15000 + parts 5000*2 + consumable 3000 = 28000
        budget.TotalPriceCents.Should().Be(28000);
        budget.IsApproved.Should().BeNull();
        budget.Jobs.Should().HaveCount(1);
        budget.Parts.Should().HaveCount(1);
        budget.Consumables.Should().HaveCount(1);
    }

    [Fact]
    public void Approve_sets_flag_and_timestamp()
    {
        var budget = ABudget(out _);

        var result = budget.Approve(Now);

        result.IsSuccess.Should().BeTrue();
        budget.IsApproved.Should().BeTrue();
        budget.ApprovedOn.Should().Be(Now);
    }

    [Fact]
    public void Approve_twice_fails_as_already_reviewed()
    {
        var budget = ABudget(out _);
        budget.Approve(Now);

        budget.Approve(Now).Error.Code.Should().Be("Budget.AlreadyReviewed");
    }

    [Fact]
    public void Reject_sets_flag_false()
    {
        var budget = ABudget(out _);

        var result = budget.Reject();

        result.IsSuccess.Should().BeTrue();
        budget.IsApproved.Should().BeFalse();
    }

    [Fact]
    public void Reject_after_review_fails()
    {
        var budget = ABudget(out _);
        budget.Approve(Now);

        budget.Reject().Error.Code.Should().Be("Budget.AlreadyReviewed");
    }

    [Fact]
    public void MarkJobAsExecuted_succeeds_for_existing_job()
    {
        var budget = ABudget(out var job);

        var result = budget.MarkJobAsExecuted(job.Id, Now);

        result.IsSuccess.Should().BeTrue();
        budget.Jobs.Single().IsExecuted.Should().BeTrue();
        budget.Jobs.Single().ExecutedOn.Should().Be(Now);
    }

    [Fact]
    public void MarkJobAsExecuted_twice_fails()
    {
        var budget = ABudget(out var job);
        budget.MarkJobAsExecuted(job.Id, Now);

        budget.MarkJobAsExecuted(job.Id, Now).Error.Code.Should().Be("BudgetJob.AlreadyExecuted");
    }

    [Fact]
    public void MarkJobAsExecuted_unknown_job_returns_not_found()
    {
        var budget = ABudget(out _);

        budget.MarkJobAsExecuted(Guid.NewGuid(), Now).Error.Code.Should().Be("BudgetJob.NotFound");
    }
}
