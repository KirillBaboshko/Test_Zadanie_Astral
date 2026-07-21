using ChatApp.Server.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatApp.Server.Infrastructure.Data.EntityConfigurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(u => u.Username)
            .HasColumnName("username")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(u => u.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(500)
            .IsRequired()
            .HasDefaultValue("temp_password_hash_needs_reset");

        builder.Property(u => u.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(u => u.LastSeenAt)
            .HasColumnName("last_seen_at")
            .IsRequired();

        builder.Metadata
            .FindNavigation(nameof(User.Messages))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);


        builder.OwnsMany(u => u.Messages, messagesBuilder =>
        {
            messagesBuilder.ToTable("messages");

            messagesBuilder.WithOwner()
                .HasForeignKey(m => m.UserId);

            messagesBuilder.Property(m => m.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();

            messagesBuilder.Property(m => m.UserId)
                .HasColumnName("user_id")
                .IsRequired();

            messagesBuilder.Property(m => m.Content)
                .HasColumnName("content")
                .HasMaxLength(1000)
                .IsRequired();

            messagesBuilder.Property(m => m.Timestamp)
                .HasColumnName("timestamp")
                .IsRequired();

            messagesBuilder.HasKey(m => m.Id);

            messagesBuilder.HasIndex(m => m.Timestamp)
                .HasDatabaseName("ix_messages_timestamp");

            messagesBuilder.HasIndex(m => m.UserId)
                .HasDatabaseName("ix_messages_user_id");
        });

        builder.HasIndex(u => u.Username)
            .IsUnique()
            .HasDatabaseName("ix_users_username");

        builder.HasIndex(u => u.LastSeenAt)
            .HasDatabaseName("ix_users_last_seen_at");
    }
}
