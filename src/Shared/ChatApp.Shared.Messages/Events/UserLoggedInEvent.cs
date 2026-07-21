namespace ChatApp.Shared.Messages.Events;

/// <summary>
/// Событие: пользователь вошёл в систему
/// Публикуется когда пользователь успешно авторизуется
/// </summary>
public record UserLoggedInEvent
{
    public Guid UserId { get; init; }
    public string Username { get; init; } = string.Empty;
    public DateTime LoggedInAt { get; init; }
}
