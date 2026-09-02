using System.Text.Json;
using ChatApp.Server.Application.Services;
using ChatApp.Server.Domain.Entities;
using ChatApp.Server.Infrastructure.Data;

namespace ChatApp.Server.Infrastructure.Services;

/// <summary>
/// Реализация Outbox сервиса для надежной публикации событий
/// Сохраняет события в БД в рамках транзакции с основными данными
/// </summary>
public class OutboxService : IOutboxService
{
    private readonly ChatDbContext _dbContext;

    public OutboxService(ChatDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task AddEventAsync<TEvent>(TEvent eventData, CancellationToken cancellationToken = default) 
        where TEvent : class
    {
        var eventType = typeof(TEvent).AssemblyQualifiedName 
            ?? throw new InvalidOperationException($"Cannot get AssemblyQualifiedName for type {typeof(TEvent).Name}");
        
        var payload = JsonSerializer.Serialize(eventData);
        
        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = eventType,
            Payload = payload,
            CreatedAt = DateTime.UtcNow,
            Status = OutboxMessageStatus.Pending,
            RetryCount = 0
        };
        
        await _dbContext.OutboxMessages.AddAsync(outboxMessage, cancellationToken);
        
    }
}
