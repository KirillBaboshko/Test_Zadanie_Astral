namespace ChatApp.Contracts.Responses;

/// <summary>
/// DTO для детальной информации о пользователе
/// </summary>
public sealed record UserInfoDto
{
    public required Guid Id { get; init; }
    public required string Username { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? LastLogin { get; init; }
    public required int MessageCount { get; init; }
}
