using ChatApp.Server.Domain.Repositories;

namespace ChatApp.Server.Application.UseCases.GetUserInfo;

/// <summary>
/// Use case для получения детальной информации о пользователе
/// </summary>
public sealed class GetUserInfoUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly IMessageRepository _messageRepository;

    public GetUserInfoUseCase(
        IUserRepository userRepository,
        IMessageRepository messageRepository)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _messageRepository = messageRepository ?? throw new ArgumentNullException(nameof(messageRepository));
    }

    /// <summary>
    /// Получает информацию о пользователе по его имени, включая количество отправленных сообщений. Возвращает null, если пользователь не найден
    /// </summary>
    public async Task<UserInfoDto?> ExecuteAsync(String username, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByUsernameAsync(username, cancellationToken);
        
        if (user == null)
            return null;

        var messageCount = await _messageRepository.GetCountForUserIdAsync(user.Id, cancellationToken);

        return new UserInfoDto
        {
            Id = user.Id,
            Username = user.Username,
            CreatedAt = user.CreatedAt,
            LastSeenAt = user.LastSeenAt,
            MessageCount = messageCount
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
