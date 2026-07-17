using ChatApp.Server.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Server.Api.BackgroundServices;


public sealed class MessageCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MessageCleanupService> _logger;
    
    private const int CheckIntervalMinutes = 1;
    private const int RetentionDays = 1;
    private const int MaxTotalMessages = 10000;

    public MessageCleanupService(
        IServiceProvider serviceProvider,
        ILogger<MessageCleanupService> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Запускает циклическую проверку и очистку сообщений
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "MessageCleanupService запущен. Интервал проверки: {Interval} мин, Срок хранения: {Retention} дн, Лимит: {Limit} сообщений",
            CheckIntervalMinutes, RetentionDays, MaxTotalMessages);

        // Даём время на запуск приложения
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(CheckIntervalMinutes), stoppingToken);
                await CleanupMessagesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("MessageCleanupService остановлен");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при очистке сообщений");
            }
        }
    }

    /// <summary>
    /// Выполняет очистку сообщений по двум критериям: время и общий лимит
    /// </summary>
    private async Task CleanupMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ChatDbContext>();

        var cutoffDate = DateTime.UtcNow.AddDays(-RetentionDays);
        var totalDeleted = 0;

        // 1. Удаляем сообщения старше 1 дня
        var deletedByTime = await context.Users
            .SelectMany(u => u.Messages)
            .Where(m => m.Timestamp < cutoffDate)
            .ExecuteDeleteAsync(cancellationToken);

        totalDeleted += deletedByTime;

        if (deletedByTime > 0)
        {
            _logger.LogInformation("Удалено {Count} сообщений старше {Days} дня", deletedByTime, RetentionDays);
        }

        // 2. Проверяем общее количество сообщений
        var currentTotalCount = await context.Users
            .SelectMany(u => u.Messages)
            .CountAsync(cancellationToken);

        if (currentTotalCount > MaxTotalMessages)
        {
            var toDeleteCount = currentTotalCount - MaxTotalMessages;
            
            _logger.LogInformation(
                "Текущее количество сообщений ({Current}) превышает лимит ({Limit}). Удаляем {ToDelete} самых старых",
                currentTotalCount, MaxTotalMessages, toDeleteCount);

            // Удаляем самые старые сообщения
            var oldestMessageIds = await context.Users
                .SelectMany(u => u.Messages)
                .OrderBy(m => m.Timestamp)
                .Take(toDeleteCount)
                .Select(m => m.Id)
                .ToListAsync(cancellationToken);

            foreach (var messageId in oldestMessageIds)
            {
                var deletedByLimit = await context.Users
                    .SelectMany(u => u.Messages)
                    .Where(m => m.Id == messageId)
                    .ExecuteDeleteAsync(cancellationToken);
                
                totalDeleted += deletedByLimit;
            }

            _logger.LogInformation("Удалено {Count} сообщений по достижению лимита", oldestMessageIds.Count);
        }

        if (totalDeleted > 0)
        {
            _logger.LogInformation(
                "Очистка завершена: всего удалено {Total} сообщений ({ByTime} по времени, {ByLimit} по лимиту)",
                totalDeleted, deletedByTime, totalDeleted - deletedByTime);
        }
        else
        {
            _logger.LogDebug("Очистка не требуется");
        }
    }
}
