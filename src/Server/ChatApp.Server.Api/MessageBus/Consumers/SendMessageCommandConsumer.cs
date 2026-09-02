using MassTransit;
using MediatR;

namespace ChatApp.Server.Api.MessageBus.Consumers;

/// <summary>
/// Consumer для обработки команды отправки сообщения
/// Использует MediatR для отправки команд с автоматическим применением Behaviors
/// </summary>
public class SendMessageCommandConsumer : IConsumer<ChatApp.Shared.Messages.Commands.SendMessageCommand>
{
    private readonly IMediator _mediator;
    private readonly ILogger<SendMessageCommandConsumer> _logger;

    public SendMessageCommandConsumer(
        IMediator mediator,
        ILogger<SendMessageCommandConsumer> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Consume(ConsumeContext<ChatApp.Shared.Messages.Commands.SendMessageCommand> context)
    {
        var rabbitCommand = context.Message;
        
        _logger.LogInformation(
            "[RabbitMQ Consumer] Получена команда SendMessage от {Username}",
            rabbitCommand.Username);

        try
        {
            var command = new ChatApp.Server.Application.Commands.SendMessage.SendMessageCommand(
                rabbitCommand.UserId,
                rabbitCommand.Content);

            var response = await _mediator.Send(command, context.CancellationToken);

            if (!response.Success)
            {
                _logger.LogWarning(
                    "[RabbitMQ Consumer] Пользователь {UserId} не найден",
                    rabbitCommand.UserId);
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
                rabbitCommand.Username);
        }
    }
}
