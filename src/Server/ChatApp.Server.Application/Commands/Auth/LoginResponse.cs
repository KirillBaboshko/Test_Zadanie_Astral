namespace ChatApp.Server.Application.Commands.Auth;

/// <summary>
/// Ответ на команду входа
/// </summary>
public sealed record LoginResponse
{
    public bool Success { get; init; }
    public string? Token { get; init; }
    public Guid UserId { get; init; }
    public string? Username { get; init; }
    public string? ErrorMessage { get; init; }
}
