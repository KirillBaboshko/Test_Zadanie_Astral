namespace ChatApp.Server.Domain.Entities;
public sealed class User
{
    private readonly List<ChatMessage> _messages = [];

    public Guid Id { get; private set; }
    public String Username { get; private set; } = String.Empty;
    public String PasswordHash { get; private set; } = String.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime LastSeenAt { get; private set; }
    public IReadOnlyList<ChatMessage> Messages => _messages;

    private User() { }

    /// <summary>
    /// Создаёт нового пользователя (для регистрации)
    /// </summary>
    public User(String username, String passwordHash)
    {
        if (String.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Имя пользователя не может быть пустым", nameof(username));

        if (String.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Хеш пароля не может быть пустым", nameof(passwordHash));

        Id = Guid.NewGuid();
        Username = username.Trim();
        PasswordHash = passwordHash;
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

    /// <summary>
    /// Изменяет пароль пользователя
    /// </summary>
    public void ChangePassword(String newPasswordHash)
    {
        if (String.IsNullOrWhiteSpace(newPasswordHash))
            throw new ArgumentException("Хеш пароля не может быть пустым", nameof(newPasswordHash));

        PasswordHash = newPasswordHash;
    }

    /// <summary>
    /// Добавляет новое сообщение к пользователю
    /// </summary>
    /// <returns>Добавленное сообщение</returns>
    public ChatMessage AddMessage(String content)
    {
        if (String.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Содержимое сообщения не может быть пустым", nameof(content));

        var message = new ChatMessage(Id, content);
        _messages.Add(message);
        UpdateLastSeen();
        
        return message;
    }

    /// <summary>
    /// Получает сообщения пользователя с ограничением
    /// </summary>
    public IReadOnlyList<ChatMessage> GetMessages(int limit = 100)
    {
        return _messages
            .OrderBy(m => m.Timestamp)
            .Take(limit)
            .ToList();
    }

    /// <summary>
    /// Получает количество сообщений пользователя
    /// </summary>
    public int GetMessageCount()
    {
        return _messages.Count;
    }
}
