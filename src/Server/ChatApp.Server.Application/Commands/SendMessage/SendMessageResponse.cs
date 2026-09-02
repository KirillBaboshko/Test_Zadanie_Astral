namespace ChatApp.Server.Application.Commands.SendMessage;

/// <summary>
/// Ответ на команду отправки сообщения
/// </summary>
public sealed record SendMessageResponse
{
    public Guid MessageId { get; init; }
    public string SenderName { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public bool Success { get; init; }
}
