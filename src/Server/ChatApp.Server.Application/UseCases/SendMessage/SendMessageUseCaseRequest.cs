namespace ChatApp.Server.Application.UseCases.SendMessage;

/// <summary>
/// Запрос для отправки сообщения
/// </summary>
public sealed class SendMessageUseCaseRequest
{
    public Guid UserId { get; init; }
    public string Content { get; init; } = string.Empty;
}
