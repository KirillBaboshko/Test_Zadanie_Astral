using System.Text.Json;
using ChatApp.Server.Domain.Entities;
using ChatApp.Server.Infrastructure.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Server.Api.BackgroundServices;

/// <summary>
/// Background Service для публикации событий из Outbox в RabbitMQ
/// Периодически читает необработанные события и публикует их в Message Bus
/// Гарантирует надежную доставку событий при сбоях
/// </summary>
public class OutboxPublisherService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxPublisherService> _logger;
    private readonly TimeSpan _processingInterval = TimeSpan.FromSeconds(5); // Обработка каждые 5 секунд
    private readonly int _batchSize = 100; // Обрабатываем до 100 событий за раз
    private readonly int _maxRetryCount = 5; // Максимум 5 попыток публикации

    public OutboxPublisherService(
        IServiceProvider serviceProvider,
        ILogger<OutboxPublisherService> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "OutboxPublisherService запущен. Интервал обработки: {Interval} сек, Размер батча: {BatchSize}",
            _processingInterval.TotalSeconds,
            _batchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обработке Outbox сообщений");
            }

            await Task.Delay(_processingInterval, stoppingToken);
        }

        _logger.LogInformation("OutboxPublisherService остановлен");
    }

    private async Task ProcessOutboxMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
        var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        // Получаем необработанные сообщения
        var messages = await dbContext.OutboxMessages
            .Where(x => x.Status == OutboxMessageStatus.Pending || 
                       (x.Status == OutboxMessageStatus.Failed && x.RetryCount < _maxRetryCount))
            .OrderBy(x => x.CreatedAt)
            .Take(_batchSize)
            .ToListAsync(cancellationToken);

        if (messages.Count == 0)
        {
            return;
        }

        _logger.LogInformation("Обработка {Count} Outbox сообщений", messages.Count);

        var publishedCount = 0;
        var failedCount = 0;

        foreach (var message in messages)
        {
            try
            {
                // Десериализуем событие
                var eventType = Type.GetType(message.EventType);
                if (eventType == null)
                {
                    _logger.LogError("Не удалось найти тип события: {EventType}", message.EventType);
                    message.Status = OutboxMessageStatus.Failed;
                    message.LastError = $"Type not found: {message.EventType}";
                    message.RetryCount++;
                    continue;
                }

                var eventData = JsonSerializer.Deserialize(message.Payload, eventType);
                if (eventData == null)
                {
                    _logger.LogError("Не удалось десериализовать событие: {EventType}", message.EventType);
                    message.Status = OutboxMessageStatus.Failed;
                    message.LastError = "Deserialization failed";
                    message.RetryCount++;
                    continue;
                }

                // Публикуем событие в RabbitMQ
                await publishEndpoint.Publish(eventData, eventType, cancellationToken);

                // Помечаем как опубликованное
                message.Status = OutboxMessageStatus.Published;
                message.PublishedAt = DateTime.UtcNow;
                message.LastError = null;
                publishedCount++;

                _logger.LogDebug(
                    "Событие опубликовано: {EventType}, Id: {MessageId}",
                    eventType.Name,
                    message.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Ошибка при публикации события: {EventType}, Id: {MessageId}",
                    message.EventType,
                    message.Id);

                message.Status = OutboxMessageStatus.Failed;
                message.LastError = ex.Message.Length > 2000 
                    ? ex.Message.Substring(0, 2000) 
                    : ex.Message;
                message.RetryCount++;
                failedCount++;
            }
        }

        // Сохраняем изменения статусов
        await dbContext.SaveChangesAsync(cancellationToken);

        if (publishedCount > 0 || failedCount > 0)
        {
            _logger.LogInformation(
                "Outbox обработка завершена: опубликовано {Published}, ошибок {Failed}",
                publishedCount,
                failedCount);
        }
    }
}
