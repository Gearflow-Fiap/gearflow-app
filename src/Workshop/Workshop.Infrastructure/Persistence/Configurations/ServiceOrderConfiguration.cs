using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Workshop.Domain.Aggregates.ServiceOrderModel;
using Workshop.Domain.ValueObjects;

namespace Workshop.Infrastructure.Persistence.Configurations;

internal sealed class ServiceOrderConfiguration : IEntityTypeConfiguration<ServiceOrder>
{
    public void Configure(EntityTypeBuilder<ServiceOrder> builder)
    {
        builder.ToTable("service_orders");

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => ServiceOrderId.From(value))
            // A chave é gerada no domínio (ServiceOrderId.New()); sem isto o EF trata a heurística
            // "chave não-default ⇒ já existe" e gera UPDATE p/ filhos novos em agregado rastreado.
            .ValueGeneratedNever();

        builder.Property(o => o.OSCode).HasColumnName("os_code").ValueGeneratedOnAdd().UseIdentityColumn();
        builder.Property(o => o.VehicleId).HasColumnName("vehicle_id");
        builder.Property(o => o.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(40);
        builder.Property(o => o.IsActive).HasColumnName("is_active");
        builder.Property(o => o.CreatedOn).HasColumnName("created_on");
        builder.Property(o => o.UpdatedOn).HasColumnName("updated_on");

        builder.OwnsOne(o => o.CreatedBy, a =>
        {
            a.Property(x => x.Type).HasColumnName("created_by_type").HasConversion<string>().HasMaxLength(20);
            a.Property(x => x.Id).HasColumnName("created_by_id");
        });

        builder.OwnsMany(o => o.RequestedJobs, j =>
        {
            j.ToTable("service_order_jobs");
            j.WithOwner().HasForeignKey("service_order_id");
            j.HasKey(x => x.Id);
            j.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            j.Property(x => x.JobId).HasColumnName("job_id");
            j.Property(x => x.CreatedOn).HasColumnName("created_on");
        });

        builder.OwnsMany(o => o.RequestedParts, p =>
        {
            p.ToTable("service_order_parts");
            p.WithOwner().HasForeignKey("service_order_id");
            p.HasKey(x => x.Id);
            p.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            p.Property(x => x.PartId).HasColumnName("part_id");
            p.Property(x => x.Quantity).HasColumnName("quantity");
            p.Property(x => x.CreatedOn).HasColumnName("created_on");
        });

        builder.OwnsMany(o => o.Histories, h =>
        {
            h.ToTable("service_order_histories");
            h.WithOwner().HasForeignKey("service_order_id");
            h.HasKey(x => x.Id);
            h.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            h.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(40);
            h.Property(x => x.Message).HasColumnName("message").HasMaxLength(500);
            h.Property(x => x.CreatedOn).HasColumnName("created_on");
            h.OwnsOne(x => x.ChangedBy, a =>
            {
                a.Property(x => x.Type).HasColumnName("changed_by_type").HasConversion<string>().HasMaxLength(20);
                a.Property(x => x.Id).HasColumnName("changed_by_id");
            });
        });

        builder.Navigation(o => o.RequestedJobs).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(o => o.RequestedParts).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(o => o.Histories).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(o => o.DomainEvents);
        builder.HasIndex(o => o.Status);
    }
}
