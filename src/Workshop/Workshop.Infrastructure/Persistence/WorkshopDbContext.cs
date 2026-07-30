using Microsoft.EntityFrameworkCore;
using Workshop.Domain.Aggregates.BudgetModel;
using Workshop.Domain.Aggregates.ServiceOrderModel;

namespace Workshop.Infrastructure.Persistence;

public sealed class WorkshopDbContext : DbContext
{
    public WorkshopDbContext(DbContextOptions<WorkshopDbContext> options) : base(options) { }

    public DbSet<ServiceOrder> ServiceOrders => Set<ServiceOrder>();
    public DbSet<Budget> Budgets => Set<Budget>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("workshop");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WorkshopDbContext).Assembly);
    }
}
