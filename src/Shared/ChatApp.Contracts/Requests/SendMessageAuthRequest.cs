namespace ChatApp.Contracts.Requests;

/// <summary>
/// Запрос на отправку сообщения от авторизованного пользователя
/// </summary>
public sealed class SendMessageAuthRequest
{
    public String Content { get; set; } = String.Empty;
}
