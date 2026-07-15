using ChatApp.Contracts.Messages;
using ChatApp.Contracts.Responses;
using ChatApp.Server.Domain.Repositories;

namespace ChatApp.Server.Application.UseCases.GetMessages;

/// <summary>
/// Use case для получения сообщений из чата
/// </summary>
public sealed class GetMessagesUseCase
{
    private readonly IMessageRepository _repository;
    private readonly IUserRepository _userRepository;

    public GetMessagesUseCase(IMessageRepository repository, IUserRepository userRepository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
    }

    /// <summary>
    /// Получает список всех сообщений с возможностью фильтрации по времени
    /// </summary>
    public async Task<GetMessagesResponse> ExecuteAsync(
        DateTime? since = null,
        Int32 limit = 100,
        CancellationToken cancellationToken = default)
    {
        var messages = await _repository.GetAsync(since, limit, cancellationToken);
        var totalCount = await _repository.GetTotalCountAsync(cancellationToken);

        return new GetMessagesResponse
        {
            Messages = messages.Select(m => new ChatMessageDto
            {
                Id = m.Id,
                SenderName = m.User.Username,
                Content = m.Content,
                Timestamp = m.Timestamp
            }).ToList(),
            TotalCount = totalCount
        };
    }

    /// <summary>
    /// Получает список сообщений конкретного пользователя по его ID
    /// </summary>
    public async Task<GetMessagesResponse> ExecuteForUserIdAsync(
        Guid userId,
        Int32 limit = 100,
        CancellationToken cancellationToken = default)
    {
        var messages = await _repository.GetForUserIdAsync(userId, limit, cancellationToken);
        var totalCount = await _repository.GetTotalCountAsync(cancellationToken);

        return new GetMessagesResponse
        {
            Messages = messages.Select(m => new ChatMessageDto
            {
                Id = m.Id,
                SenderName = m.User.Username,
                Content = m.Content,
                Timestamp = m.Timestamp
            }).ToList(),
            TotalCount = totalCount
        };
    }

    /// <summary>
    /// Получает список сообщений конкретного пользователя по его имени. Возвращает null, если пользователь не найден
    /// </summary>
    public async Task<GetMessagesResponse?> ExecuteForUsernameAsync(
        String username,
        Int32 limit = 100,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByUsernameAsync(username, cancellationToken);
        
        if (user == null)
            return null;

        return await ExecuteForUserIdAsync(user.Id, limit, cancellationToken);
    }
}
