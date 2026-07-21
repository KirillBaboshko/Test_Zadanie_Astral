using ChatApp.Server.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatApp.Server.Infrastructure.Data.Configurations;

/// <summary>
/// Конфигурация EF Core для сущности OutboxMessage
/// </summary>
public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");
        
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.EventType)
            .IsRequired()
            .HasMaxLength(500);
        
        builder.Property(x => x.Payload)
            .IsRequired()
            .HasColumnType("text");
        
        builder.Property(x => x.CreatedAt)
            .IsRequired();
        
        builder.Property(x => x.PublishedAt)
            .IsRequired(false);
        
        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<int>();
        
        builder.Property(x => x.RetryCount)
            .IsRequired()
            .HasDefaultValue(0);
        
        builder.Property(x => x.LastError)
            .IsRequired(false)
            .HasMaxLength(2000);
        
        // Индекс для быстрого поиска необработанных сообщений
        builder.HasIndex(x => new { x.Status, x.CreatedAt })
            .HasDatabaseName("idx_outbox_status_created");
    }
}
