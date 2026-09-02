using ChatApp.Contracts.Messages;
using ChatApp.Contracts.Responses;
using ChatApp.Server.Domain.Repositories;
using MediatR;

namespace ChatApp.Server.Application.Queries.GetMessages;

/// <summary>
/// Handler для получения сообщений пользователя по имени
/// </summary>
public sealed class GetMessagesByUsernameQueryHandler : IRequestHandler<GetMessagesByUsernameQuery, GetMessagesResponse?>
{
    private readonly IUserRepository _userRepository;

    public GetMessagesByUsernameQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<GetMessagesResponse?> Handle(GetMessagesByUsernameQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByUsernameWithMessagesAsync(request.Username, cancellationToken);

        if (user == null)
        {
            return null;
        }

        var messages = user.Messages
            .OrderBy(m => m.Timestamp)
            .Take(request.Limit)
            .Select(m => new ChatMessageDto
            {
                Id = m.Id,
                SenderName = user.Username,
                Content = m.Content,
                Timestamp = m.Timestamp
            })
            .ToList();

        return new GetMessagesResponse
        {
            Messages = messages,
            TotalCount = messages.Count
        };
    }
}
