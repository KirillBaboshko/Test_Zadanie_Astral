namespace ChatApp.Shared.Messages.Commands;

/// <summary>
/// Команда: отправить сообщение
/// Отправляется клиентом для отправки нового сообщения в чат
/// </summary>
public record SendMessageCommand
{
    public Guid UserId { get; init; }
    public string Username { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
}
