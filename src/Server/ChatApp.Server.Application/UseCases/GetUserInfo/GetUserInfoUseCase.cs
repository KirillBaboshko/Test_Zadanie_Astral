using ChatApp.Server.Domain.Repositories;

namespace ChatApp.Server.Application.UseCases.GetUserInfo;


public sealed class GetUserInfoUseCase
{
    private readonly IUserRepository _userRepository;

    public GetUserInfoUseCase(IUserRepository userRepository)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
    }

    /// <summary>
    /// Получает информацию о пользователе по его имени, включая количество отправленных сообщений. Возвращает null, если пользователь не найден
    /// </summary>
    public async Task<UserInfoDto?> ExecuteAsync(String username, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByUsernameWithMessagesAsync(username, cancellationToken);
        
        if (user == null)
            return null;

        return new UserInfoDto
        {
            Id = user.Id,
            Username = user.Username,
            CreatedAt = user.CreatedAt,
            LastSeenAt = user.LastSeenAt,
            MessageCount = user.GetMessageCount()
        };
    }
}

/// <summary>
/// DTO для передачи расширенной информации о пользователе, включая статистику
/// </summary>
public sealed class UserInfoDto
{
    public Guid Id { get; set; }
    public String Username { get; set; } = String.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime LastSeenAt { get; set; }
    public Int32 MessageCount { get; set; }
}
