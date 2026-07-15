using ChatApp.Server.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatApp.Server.Infrastructure.Data.EntityConfigurations;

public sealed class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.ToTable("messages");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(m => m.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(m => m.Content)
            .HasColumnName("content")
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(m => m.Timestamp)
            .HasColumnName("timestamp")
            .IsRequired();

        builder.HasIndex(m => m.Timestamp)
            .HasDatabaseName("ix_messages_timestamp");

        builder.HasIndex(m => m.UserId)
            .HasDatabaseName("ix_messages_user_id");
    }
}
