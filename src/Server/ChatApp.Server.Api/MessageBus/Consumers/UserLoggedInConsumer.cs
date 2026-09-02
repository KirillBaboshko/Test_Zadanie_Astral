using ChatApp.Shared.Messages.Events;
using MassTransit;

namespace ChatApp.Server.Api.MessageBus.Consumers;

/// <summary>
/// Consumer для обработки события входа пользователя
/// Логирует активность для аналитики и безопасности
/// </summary>
public class UserLoggedInConsumer : IConsumer<UserLoggedInEvent>
{
    private readonly ILogger<UserLoggedInConsumer> _logger;

    public UserLoggedInConsumer(ILogger<UserLoggedInConsumer> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task Consume(ConsumeContext<UserLoggedInEvent> context)
    {
        var message = context.Message;
        
        _logger.LogInformation(
            "[RabbitMQ Consumer] Пользователь вошёл в систему: {Username} (Id={UserId}), Время={LoggedInAt}",
            message.Username,
            message.UserId,
            message.LoggedInAt);
        
        
        return Task.CompletedTask;
    }
}
