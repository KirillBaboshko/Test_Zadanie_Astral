namespace ChatApp.Server.Domain.Entities;

/// <summary>
/// Сущность пользователя чата
/// </summary>
public sealed class User
{
    public Guid Id { get; private set; }
    public String Username { get; private set; } = String.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime LastSeenAt { get; private set; }

    // Navigation property
    public ICollection<ChatMessage> Messages { get; private set; } = new List<ChatMessage>();

    private User() { }

    /// <summary>
    /// Создаёт нового пользователя
    /// </summary>
    public User(String username)
    {
        if (String.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Имя пользователя не может быть пустым", nameof(username));

        Id = Guid.NewGuid();
        Username = username.Trim();
        CreatedAt = DateTime.UtcNow;
        LastSeenAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Обновляет время последней активности пользователя
    /// </summary>
    public void UpdateLastSeen()
    {
        LastSeenAt = DateTime.UtcNow;
    }
}
