namespace ChatApp.Server.Application.Commands.Auth;

/// <summary>
/// Ответ на команду регистрации
/// </summary>
public sealed record RegisterResponse
{
    public bool Success { get; init; }
    public string? Token { get; init; }
    public Guid UserId { get; init; }
    public string? Username { get; init; }
    public string? ErrorMessage { get; init; }
}
