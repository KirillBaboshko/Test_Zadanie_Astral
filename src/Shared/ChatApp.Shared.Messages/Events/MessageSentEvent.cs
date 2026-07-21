namespace ChatApp.Shared.Messages.Events;

/// <summary>
/// Событие: сообщение отправлено
/// Публикуется когда пользователь отправляет сообщение в чат
/// </summary>
public record MessageSentEvent
{
    public Guid MessageId { get; init; }
    public Guid SenderId { get; init; }
    public string SenderName { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
}
