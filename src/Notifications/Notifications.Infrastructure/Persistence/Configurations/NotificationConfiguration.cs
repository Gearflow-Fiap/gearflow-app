using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notifications.Domain.Aggregates;

namespace Notifications.Infrastructure.Persistence.Configurations;

internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");

        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).HasColumnName("id");
        builder.Property(n => n.Type).HasColumnName("type").HasConversion<string>().HasMaxLength(30);
        builder.Property(n => n.Channel).HasColumnName("channel").HasConversion<string>().HasMaxLength(30);
        builder.Property(n => n.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
        builder.Property(n => n.Message).HasColumnName("message").HasMaxLength(2000).IsRequired();
        builder.Property(n => n.From).HasColumnName("from_address").HasMaxLength(320);
        builder.Property(n => n.To).HasColumnName("to_address").HasMaxLength(320);
        builder.Property(n => n.DataJson).HasColumnName("data_json");
        builder.Property(n => n.SentOn).HasColumnName("sent_on");

        builder.Ignore(n => n.DomainEvents);
    }
}
