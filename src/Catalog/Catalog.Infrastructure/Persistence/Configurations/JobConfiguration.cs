using Catalog.Domain.Aggregates;
using Catalog.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.Persistence.Configurations;

internal sealed class JobConfiguration : IEntityTypeConfiguration<Job>
{
    public void Configure(EntityTypeBuilder<Job> builder)
    {
        builder.ToTable("jobs");

        builder.HasKey(j => j.Id);
        builder.Property(j => j.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => JobId.From(value));

        builder.Property(j => j.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(j => j.Description).HasColumnName("description").HasMaxLength(1000);
        builder.Property(j => j.PriceCents).HasColumnName("price_cents").IsRequired();
        builder.Property(j => j.CreatedOn).HasColumnName("created_on").IsRequired();

        builder.Ignore(j => j.DomainEvents);
    }
}
