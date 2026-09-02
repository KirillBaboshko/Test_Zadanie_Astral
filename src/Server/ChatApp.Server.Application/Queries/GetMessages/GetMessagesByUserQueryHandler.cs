using ChatApp.Contracts.Messages;
using ChatApp.Contracts.Responses;
using ChatApp.Server.Domain.Repositories;
using MediatR;

namespace ChatApp.Server.Application.Queries.GetMessages;

/// <summary>
/// Handler для получения сообщений пользователя по ID
/// </summary>
public sealed class GetMessagesByUserQueryHandler : IRequestHandler<GetMessagesByUserQuery, GetMessagesResponse>
{
    private readonly IUserRepository _userRepository;

    public GetMessagesByUserQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<GetMessagesResponse> Handle(GetMessagesByUserQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdWithMessagesAsync(request.UserId, cancellationToken);

        if (user == null)
        {
            return new GetMessagesResponse
            {
                Messages = new List<ChatMessageDto>(),
                TotalCount = 0
            };
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
