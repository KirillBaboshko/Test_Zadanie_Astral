using ChatApp.Shared.Messages.Events;
using MassTransit;

namespace ChatApp.Client.Infrastructure.MessageBus.Consumers;

/// <summary>
/// Consumer для получения событий о регистрации новых пользователей
/// Тихо обрабатывает события без вывода в консоль
/// </summary>
public class UserRegisteredEventConsumer : IConsumer<UserRegisteredEvent>
{
    public Task Consume(ConsumeContext<UserRegisteredEvent> context)
    {
        return Task.CompletedTask;
    }
}
