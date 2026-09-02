using ChatApp.Server.Application.Services;
using ChatApp.Server.Domain.Repositories;
using ChatApp.Shared.Messages.Events;
using MediatR;

namespace ChatApp.Server.Application.Commands.SendMessage;

/// <summary>
/// Handler для команды отправки сообщения
/// Бизнес-логика выполняется здесь, декораторы (Behaviors) применяются автоматически
/// </summary>
public sealed class SendMessageCommandHandler : IRequestHandler<SendMessageCommand, SendMessageResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IOutboxService _outboxService;

    public SendMessageCommandHandler(
        IUserRepository userRepository,
        IOutboxService outboxService)
    {
        _userRepository = userRepository;
        _outboxService = outboxService;
    }

    public async Task<SendMessageResponse> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        // Находим пользователя
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user == null)
        {
            return new SendMessageResponse { Success = false };
        }

        // Добавляем сообщение
        user.UpdateLastSeen();
        var message = user.AddMessage(request.Content);

        // Добавляем событие в Outbox (в той же транзакции)
        await _outboxService.AddEventAsync(new MessageSentEvent
        {
            MessageId = message.Id,
            SenderId = user.Id,
            SenderName = user.Username,
            Content = message.Content,
            Timestamp = message.Timestamp
        }, cancellationToken);

        return new SendMessageResponse
        {
            MessageId = message.Id,
            SenderName = user.Username,
            Content = message.Content,
            Timestamp = message.Timestamp,
            Success = true
        };
    }
}
