using ChatApp.Shared.Messages.Events;
using MassTransit;

namespace ChatApp.Server.Api.MessageBus.Consumers;

/// <summary>
/// Consumer для обработки события регистрации пользователя
/// Выполняет welcome-действия для новых пользователей
/// </summary>
public class UserRegisteredConsumer : IConsumer<UserRegisteredEvent>
{
    private readonly ILogger<UserRegisteredConsumer> _logger;

    public UserRegisteredConsumer(ILogger<UserRegisteredConsumer> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task Consume(ConsumeContext<UserRegisteredEvent> context)
    {
        var message = context.Message;
        
        _logger.LogInformation(
            "[RabbitMQ Consumer] Новый пользователь зарегистрирован: {Username} (Id={UserId}), Время={RegisteredAt}",
            message.Username,
            message.UserId,
            message.RegisteredAt);
        
        
        return Task.CompletedTask;
    }
}
