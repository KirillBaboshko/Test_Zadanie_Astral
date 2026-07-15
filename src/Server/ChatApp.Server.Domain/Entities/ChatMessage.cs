namespace ChatApp.Server.Domain.Entities;

/// <summary>
/// Сущность сообщения в чате
/// </summary>
public sealed class ChatMessage
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public String Content { get; private set; } = String.Empty;
    public DateTime Timestamp { get; private set; }

    // Navigation property
    public User User { get; private set; } = null!;

    private ChatMessage() { } 

    /// <summary>
    /// Создаёт новое сообщение от пользователя
    /// </summary>
    public ChatMessage(Guid userId, String content)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("ID пользователя не может быть пустым", nameof(userId));

        if (String.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Содержимое не может быть пустым", nameof(content));

        Id = Guid.NewGuid();
        UserId = userId;
        Content = content.Trim();
        Timestamp = DateTime.UtcNow;
    }
}
