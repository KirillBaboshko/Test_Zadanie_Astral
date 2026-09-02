using ChatApp.Contracts.Messages;
using ChatApp.Contracts.Responses;
using ChatApp.Server.Domain.Repositories;
using MediatR;

namespace ChatApp.Server.Application.Queries.GetMessages;

/// <summary>
/// Handler для получения списка сообщений
/// </summary>
public sealed class GetMessagesQueryHandler : IRequestHandler<GetMessagesQuery, GetMessagesResponse>
{
    private readonly IUserRepository _userRepository;

    public GetMessagesQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<GetMessagesResponse> Handle(GetMessagesQuery request, CancellationToken cancellationToken)
    {
        var users = await _userRepository.GetAllAsync(cancellationToken);

        var allMessages = users
            .SelectMany(u => u.Messages.Select(m => new ChatMessageDto
            {
                Id = m.Id,
                SenderName = u.Username,
                Content = m.Content,
                Timestamp = m.Timestamp
            }))
            .Where(m => !request.Since.HasValue || m.Timestamp > request.Since.Value)
            .OrderBy(m => m.Timestamp)
            .Take(request.Limit)
            .ToList();

        return new GetMessagesResponse
        {
            Messages = allMessages,
            TotalCount = allMessages.Count
        };
    }
}
