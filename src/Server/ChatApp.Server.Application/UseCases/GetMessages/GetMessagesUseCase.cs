using ChatApp.Contracts.Messages;
using ChatApp.Contracts.Responses;
using ChatApp.Server.Domain.Repositories;

namespace ChatApp.Server.Application.UseCases.GetMessages;

public sealed class GetMessagesUseCase
{
    private readonly IMessageRepository _repository;

    public GetMessagesUseCase(IMessageRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

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
                SenderName = m.SenderName,
                Content = m.Content,
                Timestamp = m.Timestamp
            }).ToList(),
            TotalCount = totalCount
        };
    }
    public async Task<GetMessagesResponse> ExecuteForNameAsync(
        Int32 limit = 100,
        String senderName = null,
        CancellationToken cancellationToken = default)
    {
        var messages = await _repository.GetForNameAsync(limit, senderName, cancellationToken);
        var totalCount = await _repository.GetTotalCountAsync(cancellationToken);

        return new GetMessagesResponse
        {
            Messages = messages.Select(m => new ChatMessageDto
            {
                Id = m.Id,
                SenderName = m.SenderName,
                Content = m.Content,
                Timestamp = m.Timestamp
            }).ToList(),
            TotalCount = totalCount
        };
    }
}
