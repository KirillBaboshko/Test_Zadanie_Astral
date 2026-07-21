using ChatApp.Contracts.Messages;
using ChatApp.Contracts.Requests;
using ChatApp.Server.Application.Common;
using ChatApp.Server.Application.Services;
using ChatApp.Server.Domain.Abstractions;
using ChatApp.Server.Domain.Entities;
using ChatApp.Server.Domain.Repositories;
using ChatApp.Shared.Messages.Events;

namespace ChatApp.Server.Application.UseCases.SendMessage;

/// <summary>
/// Use case для отправки сообщений
/// Использует Outbox для надежной публикации событий
/// </summary>
public sealed class SendMessageUseCase : UseCaseBase
{
    private readonly IUserRepository _userRepository;
    private readonly IOutboxService _outboxService;

    public SendMessageUseCase(
        IUserRepository userRepository,
        IOutboxService outboxService,
        IUnitOfWork unitOfWork) : base(unitOfWork)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _outboxService = outboxService ?? throw new ArgumentNullException(nameof(outboxService));
    }

    /// <summary>
    /// Отправляет новое сообщение от авторизованного пользователя (по userId из JWT)
    /// Сохраняет сообщение и событие в одной транзакции через Outbox
    /// </summary>
    public async Task<ChatMessageDto?> ExecuteAuthAsync(Guid userId, SendMessageAuthRequest request, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithUnitOfWorkAsync(async ct =>
        {
            var user = await _userRepository.GetByIdAsync(userId, ct);
            
            if (user == null)
                return null;

            user.UpdateLastSeen();
            var message = user.AddMessage(request.Content);

            // Добавляем событие в Outbox в той же транзакции
            // Событие будет опубликовано OutboxPublisherService
            await _outboxService.AddEventAsync(new MessageSentEvent
            {
                MessageId = message.Id,
                SenderId = user.Id,
                SenderName = user.Username,
                Content = message.Content,
                Timestamp = message.Timestamp
            }, ct);

            return new ChatMessageDto
            {
                Id = message.Id,
                SenderName = user.Username,
                Content = message.Content,
                Timestamp = message.Timestamp
            };
        }, cancellationToken);
    }
}
