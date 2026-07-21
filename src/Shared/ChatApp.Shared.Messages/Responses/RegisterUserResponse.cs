namespace ChatApp.Shared.Messages.Responses;

/// <summary>
/// Ответ на команду регистрации
/// Используется в Request-Response паттерне
/// </summary>
public record RegisterUserResponse
{
    public bool Success { get; init; }
    public string Token { get; init; } = string.Empty;
    public Guid UserId { get; init; }
    public string Username { get; init; } = string.Empty;
    public string ErrorMessage { get; init; } = string.Empty;
}
