using ChatApp.Contracts.Messages;
using ChatApp.Contracts.Responses;
using ChatApp.Server.Domain.Repositories;

namespace ChatApp.Server.Application.UseCases.GetMessages;


public sealed class GetMessagesUseCase
{
    private readonly IUserRepository _userRepository;

    public GetMessagesUseCase(IUserRepository userRepository)
    {
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
        var users = await _userRepository.GetAllUsersWithMessagesAsync(cancellationToken);
        var totalCount = await _userRepository.GetTotalMessageCountAsync(cancellationToken);

        var allMessages = users
            .SelectMany(user => user.Messages.Select(message => new
            {
                User = user,
                Message = message
            }))
            .Where(x => !since.HasValue || x.Message.Timestamp >= since.Value)
            .OrderBy(x => x.Message.Timestamp)
            .Take(limit)
            .Select(x => new ChatMessageDto
            {
                Id = x.Message.Id,
                SenderName = x.User.Username,
                Content = x.Message.Content,
                Timestamp = x.Message.Timestamp
            })
            .ToList();

        return new GetMessagesResponse
        {
            Messages = allMessages,
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
        var user = await _userRepository.GetByIdWithMessagesAsync(userId, cancellationToken);
        
        if (user == null)
        {
            return new GetMessagesResponse
            {
                Messages = new List<ChatMessageDto>(),
                TotalCount = 0
            };
        }

        var messages = user.GetMessages(limit);
        var totalCount = await _userRepository.GetTotalMessageCountAsync(cancellationToken);

        return new GetMessagesResponse
        {
            Messages = messages.Select(m => new ChatMessageDto
            {
                Id = m.Id,
                SenderName = user.Username,
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
        var user = await _userRepository.GetByUsernameWithMessagesAsync(username, cancellationToken);
        
        if (user == null)
            return null;

        var messages = user.GetMessages(limit);
        var totalCount = await _userRepository.GetTotalMessageCountAsync(cancellationToken);

        return new GetMessagesResponse
        {
            Messages = messages.Select(m => new ChatMessageDto
            {
                Id = m.Id,
                SenderName = user.Username,
                Content = m.Content,
                Timestamp = m.Timestamp
            }).ToList(),
            TotalCount = totalCount
        };
    }
}
