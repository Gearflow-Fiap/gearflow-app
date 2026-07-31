using Customers.Domain.Aggregates;
using Customers.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Customers.Infrastructure.Persistence.Configurations;

internal sealed class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.ToTable("vehicles");

        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => VehicleId.From(value))
            .ValueGeneratedNever();

        builder.Property(v => v.ClientId)
            .HasColumnName("client_id")
            .HasConversion(id => id.Value, value => ClientId.From(value));

        builder.Property(v => v.LicensePlate).HasColumnName("license_plate").HasMaxLength(10).IsRequired();
        builder.Property(v => v.Mark).HasColumnName("mark").HasMaxLength(80);
        builder.Property(v => v.Model).HasColumnName("model").HasMaxLength(80);
        builder.Property(v => v.Color).HasColumnName("color").HasMaxLength(40);
        builder.Property(v => v.YearFabrication).HasColumnName("year_fabrication");
        builder.Property(v => v.YearModel).HasColumnName("year_model");
        builder.Property(v => v.CreatedOn).HasColumnName("created_on");
        builder.Property(v => v.CreatedById).HasColumnName("created_by_id");

        builder.HasIndex(v => v.LicensePlate);
    }
}
