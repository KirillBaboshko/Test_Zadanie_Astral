namespace ChatApp.Shared.Messages.Commands;

/// <summary>
/// Команда: войти в систему
/// Отправляется клиентом для аутентификации
/// Request-Response паттерн: ожидает ответ с токеном
/// </summary>
public record LoginUserCommand
{
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}
