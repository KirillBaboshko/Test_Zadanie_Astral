using ChatApp.Shared.Messages.Events;
using MassTransit;

namespace ChatApp.Server.Api.MessageBus.Consumers;

/// <summary>
/// Consumer для обработки события отправки сообщения
/// Логирует все отправленные сообщения для аналитики
/// </summary>
public class MessageSentConsumer : IConsumer<MessageSentEvent>
{
    private readonly ILogger<MessageSentConsumer> _logger;

    public MessageSentConsumer(ILogger<MessageSentConsumer> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task Consume(ConsumeContext<MessageSentEvent> context)
    {
        var message = context.Message;
        
        _logger.LogInformation(
            "[RabbitMQ Consumer] Сообщение отправлено: Id={MessageId}, Отправитель={SenderName} ({SenderId}), Время={Timestamp}",
            message.MessageId,
            message.SenderName,
            message.SenderId,
            message.Timestamp);
        
        // Здесь может быть логика:
        // - Сохранение в аналитическую БД
        // - Отправка уведомлений другим пользователям
        // - Модерация контента
        // - Обновление статистики активности
        
        return Task.CompletedTask;
    }
}
