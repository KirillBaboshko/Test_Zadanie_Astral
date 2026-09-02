namespace ChatApp.Contracts.Responses;

/// <summary>
/// DTO для представления пользователя
/// </summary>
public sealed record UserDto
{
    public required Guid Id { get; init; }
    public required string Username { get; init; }
}
