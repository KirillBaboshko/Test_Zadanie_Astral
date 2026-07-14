using ChatApp.Contracts.Messages;
using ChatApp.Contracts.Requests;
using ChatApp.Server.Domain.Entities;
using ChatApp.Server.Domain.Repositories;

namespace ChatApp.Server.Application.UseCases.SendMessage;

public sealed class SendMessageUseCase
{
    private readonly IMessageRepository _repository;

    public SendMessageUseCase(IMessageRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<ChatMessageDto> ExecuteAsync(SendMessageRequest request, CancellationToken cancellationToken = default)
    {
        var message = new ChatMessage(request.SenderName, request.Content);
        var saved = await _repository.AddAsync(message, cancellationToken);

        return new ChatMessageDto
        {
            Id = saved.Id,
            SenderName = saved.SenderName,
            Content = saved.Content,
            Timestamp = saved.Timestamp
        };
    }
}
