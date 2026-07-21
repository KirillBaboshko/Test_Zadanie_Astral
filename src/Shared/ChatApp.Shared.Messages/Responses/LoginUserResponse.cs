namespace ChatApp.Shared.Messages.Responses;

/// <summary>
/// Ответ на команду входа в систему
/// Используется в Request-Response паттерне
/// </summary>
public record LoginUserResponse
{
    public bool Success { get; init; }
    public string Token { get; init; } = string.Empty;
    public Guid UserId { get; init; }
    public string Username { get; init; } = string.Empty;
    public string ErrorMessage { get; init; } = string.Empty;
}
