namespace ChatApp.Shared.Messages.Events;

/// <summary>
/// Событие: пользователь зарегистрирован
/// Публикуется когда новый пользователь успешно регистрируется в системе
/// </summary>
public record UserRegisteredEvent
{
    public Guid UserId { get; init; }
    public string Username { get; init; } = string.Empty;
    public DateTime RegisteredAt { get; init; }
}
