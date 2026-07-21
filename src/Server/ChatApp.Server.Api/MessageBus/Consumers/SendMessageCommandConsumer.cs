using ChatApp.Server.Application.Common;
using ChatApp.Server.Application.UseCases.SendMessage;
using ChatApp.Shared.Messages.Commands;
using MassTransit;

namespace ChatApp.Server.Api.MessageBus.Consumers;

/// <summary>
/// Consumer для обработки команды отправки сообщения
/// Использует декорированный Use Case с Cross-Cutting Concerns
/// </summary>
public class SendMessageCommandConsumer : IConsumer<SendMessageCommand>
{
    private readonly IUseCase<SendMessageUseCaseRequest, SendMessageUseCaseResponse> _sendMessageUseCase;
    private readonly ILogger<SendMessageCommandConsumer> _logger;

    public SendMessageCommandConsumer(
        IUseCase<SendMessageUseCaseRequest, SendMessageUseCaseResponse> sendMessageUseCase,
        ILogger<SendMessageCommandConsumer> logger)
    {
        _sendMessageUseCase = sendMessageUseCase ?? throw new ArgumentNullException(nameof(sendMessageUseCase));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Consume(ConsumeContext<SendMessageCommand> context)
    {
        var command = context.Message;
        
        _logger.LogInformation(
            "[RabbitMQ Consumer] Получена команда SendMessage от {Username}",
            command.Username);

        try
        {
            var request = new SendMessageUseCaseRequest
            {
                UserId = command.UserId,
                Content = command.Content
            };

            // Вызываем декорированный Use Case
            // Автоматически применяются: Logging -> UnitOfWork -> Core Logic
            var response = await _sendMessageUseCase.ExecuteAsync(request, context.CancellationToken);

            if (!response.Success)
            {
                _logger.LogWarning(
                    "[RabbitMQ Consumer] Пользователь {UserId} не найден",
                    command.UserId);
                return;
            }

            _logger.LogInformation(
                "[RabbitMQ Consumer] Сообщение обработано: Id={MessageId}",
                response.MessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "[RabbitMQ Consumer] Ошибка при обработке команды от {Username}",
                command.Username);
        }
    }
}
