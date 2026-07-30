using Identity.Domain.Aggregates;
using Identity.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.HasKey(rt => rt.Id);
        builder.Property(rt => rt.Id).HasColumnName("id");

        builder.Property(rt => rt.UserId)
            .HasColumnName("user_id")
            .HasConversion(id => id.Value, value => UserId.From(value));

        builder.Property(rt => rt.TokenHash).HasColumnName("token_hash").HasMaxLength(128).IsRequired();
        builder.Property(rt => rt.ExpiresAt).HasColumnName("expires_at");
        builder.Property(rt => rt.RevokedAt).HasColumnName("revoked_at");
        builder.Property(rt => rt.ReplacedByTokenHash).HasColumnName("replaced_by_token_hash").HasMaxLength(128);
        builder.Property(rt => rt.CreatedByIp).HasColumnName("created_by_ip").HasMaxLength(64);
        builder.Property(rt => rt.RevokedByIp).HasColumnName("revoked_by_ip").HasMaxLength(64);
        builder.Property(rt => rt.CreatedOn).HasColumnName("created_on");
        builder.Property(rt => rt.UpdatedOn).HasColumnName("updated_on");

        builder.HasIndex(rt => rt.TokenHash);
    }
}
