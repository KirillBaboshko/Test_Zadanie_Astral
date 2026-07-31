using ChatApp.Server.Application.Common;
using ChatApp.Server.Application.Common.CrossCutting;
using ChatApp.Server.Application.Services;
using ChatApp.Server.Domain.Repositories;
using ChatApp.Shared.Messages.Events;

namespace ChatApp.Server.Application.UseCases.SendMessage;

/// <summary>
/// Use Case для отправки сообщений с применением Cross-Cutting Concerns
/// Декораторы: Logging -> UnitOfWork -> Core Logic
/// </summary>
public sealed class SendMessageUseCase : DecoratedUseCase<SendMessageUseCaseRequest, SendMessageUseCaseResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IOutboxService _outboxService;

    public SendMessageUseCase(
        IUserRepository userRepository,
        IOutboxService outboxService,
        IEnumerable<IUseCaseDecorator<SendMessageUseCaseRequest, SendMessageUseCaseResponse>> decorators)
        : base(decorators)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _outboxService = outboxService ?? throw new ArgumentNullException(nameof(outboxService));
    }

    /// <summary>
    /// Основная бизнес-логика отправки сообщения
    /// Выполняется внутри транзакции и с логированием (через декораторы)
    /// </summary>
    protected override async Task<SendMessageUseCaseResponse> ExecuteCoreAsync(
        SendMessageUseCaseRequest request,
        CancellationToken cancellationToken)
    {
        // Находим пользователя
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user == null)
        {
            return new SendMessageUseCaseResponse
            {
                Success = false
            };
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

        return new SendMessageUseCaseResponse
        {
            MessageId = message.Id,
            SenderName = user.Username,
            Content = message.Content,
            Timestamp = message.Timestamp,
            Success = true
        };
    }
}
