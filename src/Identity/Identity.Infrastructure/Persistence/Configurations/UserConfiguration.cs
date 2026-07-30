using Identity.Domain.Aggregates;
using Identity.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => UserId.From(value));

        builder.Property(u => u.Email).HasColumnName("email").HasMaxLength(320).IsRequired();
        builder.Property(u => u.NormalizedEmail).HasColumnName("normalized_email").HasMaxLength(320).IsRequired();
        builder.Property(u => u.UserName).HasColumnName("user_name").HasMaxLength(120).IsRequired();
        builder.Property(u => u.NormalizedUserName).HasColumnName("normalized_user_name").HasMaxLength(120).IsRequired();
        builder.Property(u => u.PasswordHash).HasColumnName("password_hash").HasMaxLength(500);
        builder.Property(u => u.EmailConfirmed).HasColumnName("email_confirmed");
        builder.Property(u => u.IsActive).HasColumnName("is_active");
        builder.Property(u => u.AccessFailedCount).HasColumnName("access_failed_count");
        builder.Property(u => u.LockoutEnd).HasColumnName("lockout_end");
        builder.Property(u => u.SecurityStamp).HasColumnName("security_stamp").HasMaxLength(64);
        builder.Property(u => u.CreatedOn).HasColumnName("created_on");
        builder.Property(u => u.UpdatedOn).HasColumnName("updated_on");

        builder.HasIndex(u => u.NormalizedEmail).IsUnique();
        builder.HasIndex(u => u.NormalizedUserName);

        builder.HasMany(u => u.RefreshTokens)
            .WithOne()
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(u => u.RefreshTokens).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(u => u.DomainEvents);
    }
}
