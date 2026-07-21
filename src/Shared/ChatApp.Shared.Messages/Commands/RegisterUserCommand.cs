namespace ChatApp.Shared.Messages.Commands;

/// <summary>
/// Команда: зарегистрировать нового пользователя
/// Отправляется клиентом для регистрации
/// </summary>
public record RegisterUserCommand
{
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}
