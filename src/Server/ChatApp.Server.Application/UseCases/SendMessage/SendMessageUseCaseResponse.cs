namespace ChatApp.Server.Application.UseCases.SendMessage;

/// <summary>
/// Ответ Use Case отправки сообщения
/// </summary>
public sealed class SendMessageUseCaseResponse
{
    public Guid MessageId { get; init; }
    public string SenderName { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public bool Success { get; init; }
}
