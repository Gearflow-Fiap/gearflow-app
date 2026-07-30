using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Workshop.Domain.Aggregates.BudgetModel;
using Workshop.Domain.ValueObjects;

namespace Workshop.Infrastructure.Persistence.Configurations;

internal sealed class BudgetConfiguration : IEntityTypeConfiguration<Budget>
{
    public void Configure(EntityTypeBuilder<Budget> builder)
    {
        builder.ToTable("budgets");

        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => BudgetId.From(value));

        builder.Property(b => b.ServiceOrderId)
            .HasColumnName("service_order_id")
            .HasConversion(id => id.Value, value => ServiceOrderId.From(value));

        builder.Property(b => b.TotalPriceCents).HasColumnName("total_price_cents");
        builder.Property(b => b.IsApproved).HasColumnName("is_approved");
        builder.Property(b => b.CreatedOn).HasColumnName("created_on");
        builder.Property(b => b.ApprovedOn).HasColumnName("approved_on");

        builder.OwnsMany(b => b.Jobs, j =>
        {
            j.ToTable("budget_jobs");
            j.WithOwner().HasForeignKey("budget_id");
            j.HasKey(x => x.Id);
            j.Property(x => x.Id).HasColumnName("id");
            j.Property(x => x.JobId).HasColumnName("job_id");
            j.Property(x => x.PriceCents).HasColumnName("price_cents");
            j.Property(x => x.IsExecuted).HasColumnName("is_executed");
            j.Property(x => x.ExecutedOn).HasColumnName("executed_on");
            j.Property(x => x.CreatedOn).HasColumnName("created_on");
        });

        builder.OwnsMany(b => b.Parts, p =>
        {
            p.ToTable("budget_parts");
            p.WithOwner().HasForeignKey("budget_id");
            p.HasKey(x => x.Id);
            p.Property(x => x.Id).HasColumnName("id");
            p.Property(x => x.PartId).HasColumnName("part_id");
            p.Property(x => x.PriceCents).HasColumnName("price_cents");
            p.Property(x => x.Quantity).HasColumnName("quantity");
            p.Property(x => x.CreatedOn).HasColumnName("created_on");
        });

        builder.OwnsMany(b => b.Consumables, c =>
        {
            c.ToTable("budget_consumables");
            c.WithOwner().HasForeignKey("budget_id");
            c.HasKey(x => x.Id);
            c.Property(x => x.Id).HasColumnName("id");
            c.Property(x => x.ConsumableId).HasColumnName("consumable_id");
            c.Property(x => x.PriceCents).HasColumnName("price_cents");
            c.Property(x => x.Quantity).HasColumnName("quantity").HasPrecision(18, 3);
            c.Property(x => x.CreatedOn).HasColumnName("created_on");
        });

        builder.Navigation(b => b.Jobs).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(b => b.Parts).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(b => b.Consumables).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(b => b.DomainEvents);
        builder.HasIndex(b => b.ServiceOrderId);
    }
}
