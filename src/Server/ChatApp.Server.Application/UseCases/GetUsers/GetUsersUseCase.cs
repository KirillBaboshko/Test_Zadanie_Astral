using ChatApp.Server.Domain.Repositories;

namespace ChatApp.Server.Application.UseCases.GetUsers;

/// <summary>
/// Use case для получения списка всех пользователей
/// </summary>
public sealed class GetUsersUseCase
{
    private readonly IUserRepository _userRepository;

    public GetUsersUseCase(IUserRepository userRepository)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
    }

    /// <summary>
    /// Получает список всех зарегистрированных пользователей
    /// </summary>
    public async Task<List<UserDto>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var users = await _userRepository.GetAllAsync(cancellationToken);

        return users.Select(u => new UserDto
        {
            Id = u.Id,
            Username = u.Username,
            CreatedAt = u.CreatedAt,
            LastSeenAt = u.LastSeenAt
        }).ToList();
    }
}

/// <summary>
/// DTO для передачи информации о пользователе
/// </summary>
public sealed class UserDto
{
    public Guid Id { get; set; }
    public String Username { get; set; } = String.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime LastSeenAt { get; set; }
}
