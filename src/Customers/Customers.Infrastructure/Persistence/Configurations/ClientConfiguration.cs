using Customers.Domain.Aggregates;
using Customers.Domain.Enums;
using Customers.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Customers.Infrastructure.Persistence.Configurations;

internal sealed class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.ToTable("clients");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => ClientId.From(value));

        builder.Property(c => c.Cpf)
            .HasColumnName("cpf").HasMaxLength(14)
            .HasConversion(v => v!.Value, v => Cpf.FromTrusted(v));

        builder.Property(c => c.Cnpj)
            .HasColumnName("cnpj").HasMaxLength(14)
            .HasConversion(v => v!.Value, v => Cnpj.FromTrusted(v));

        builder.Property(c => c.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(c => c.Phone).HasColumnName("phone").HasMaxLength(30);

        builder.Property(c => c.Status)
            .HasColumnName("status").HasConversion<string>().HasMaxLength(20)
            .HasDefaultValue(ClientStatus.Active)
            .IsRequired();

        builder.Property(c => c.Email)
            .HasColumnName("email").HasMaxLength(320).IsRequired()
            .HasConversion(v => v.Value, v => Email.FromTrusted(v));

        builder.OwnsOne(c => c.Address, a =>
        {
            a.Property(p => p.Street).HasColumnName("address_street").HasMaxLength(200);
            a.Property(p => p.City).HasColumnName("address_city").HasMaxLength(120);
            a.Property(p => p.State).HasColumnName("address_state").HasMaxLength(120);
            a.Property(p => p.Country).HasColumnName("address_country").HasMaxLength(120);
            a.Property(p => p.ZipCode).HasColumnName("address_zipcode").HasMaxLength(20);
        });
        builder.Navigation(c => c.Address).IsRequired();

        builder.HasMany(c => c.Vehicles)
            .WithOne()
            .HasForeignKey(v => v.ClientId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Metadata.FindNavigation(nameof(Client.Vehicles))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(c => c.DomainEvents);

        builder.HasIndex(c => c.Cpf);
        builder.HasIndex(c => c.Cnpj);
    }
}
