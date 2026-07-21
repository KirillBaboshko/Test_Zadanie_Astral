using ChatApp.Contracts.Requests;
using ChatApp.Server.Application.UseCases.SendMessage;
using ChatApp.Shared.Messages.Commands;
using MassTransit;

namespace ChatApp.Server.Api.MessageBus.Consumers;

/// <summary>
/// Consumer для обработки команды отправки сообщения
/// Fire-and-forget паттерн: UseCase сам сохраняет сообщение и событие в Outbox
/// </summary>
public class SendMessageCommandConsumer : IConsumer<SendMessageCommand>
{
    private readonly SendMessageUseCase _sendMessageUseCase;
    private readonly ILogger<SendMessageCommandConsumer> _logger;

    public SendMessageCommandConsumer(
        SendMessageUseCase sendMessageUseCase,
        ILogger<SendMessageCommandConsumer> logger)
    {
        _sendMessageUseCase = sendMessageUseCase ?? throw new ArgumentNullException(nameof(sendMessageUseCase));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Consume(ConsumeContext<SendMessageCommand> context)
    {
        var command = context.Message;
        
        _logger.LogInformation(
            "[RabbitMQ Command Consumer] Получена команда отправки сообщения: От={Username}, Контент={Content}",
            command.Username,
            command.Content.Length > 50 ? command.Content.Substring(0, 50) + "..." : command.Content);

        try
        {
            var request = new SendMessageAuthRequest
            {
                Content = command.Content
            };

            var messageDto = await _sendMessageUseCase.ExecuteAuthAsync(
                command.UserId, 
                request, 
                context.CancellationToken);

            if (messageDto == null)
            {
                _logger.LogWarning(
                    "[RabbitMQ Command Consumer] Не удалось отправить сообщение: пользователь {UserId} не найден",
                    command.UserId);
                return;
            }

            _logger.LogInformation(
                "[RabbitMQ Command Consumer] Сообщение сохранено в БД и Outbox: Id={MessageId}, От={Username}",
                messageDto.Id,
                command.Username);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "[RabbitMQ Command Consumer] Ошибка при отправке сообщения от {Username}",
                command.Username);
        }
    }
}
