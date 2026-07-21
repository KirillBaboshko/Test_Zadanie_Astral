using ChatApp.Client.Infrastructure.MessageBus;
using ChatApp.Shared.Messages.Events;
using MassTransit;

namespace ChatApp.Client.Infrastructure.MessageBus.Consumers;

/// <summary>
/// Consumer для получения событий о новых сообщениях
/// Отображает сообщения в реальном времени (кроме своих собственных)
/// </summary>
public class MessageSentEventConsumer : IConsumer<MessageSentEvent>
{
    public Task Consume(ConsumeContext<MessageSentEvent> context)
    {
        var message = context.Message;
        
        if (CurrentUserContext.Instance.IsCurrentUser(message.SenderId))
        {
            return Task.CompletedTask;
        }
        
        var timestamp = message.Timestamp.ToLocalTime().ToString("HH:mm:ss");
        Console.WriteLine($"\n[{timestamp}] {message.SenderName}: {message.Content}");
        Console.Write("> ");
        
        return Task.CompletedTask;
    }
}
