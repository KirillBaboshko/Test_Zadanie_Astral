namespace ChatApp.Server.Domain.Entities;

/// <summary>
/// Outbox сообщение для надежной публикации событий в Message Bus
/// Паттерн Outbox гарантирует атомарность сохранения данных и публикации событий
/// </summary>
public class OutboxMessage
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Тип события (полное имя типа для десериализации)
    /// </summary>
    public string EventType { get; set; } = string.Empty;
    
    /// <summary>
    /// JSON payload события
    /// </summary>
    public string Payload { get; set; } = string.Empty;
    
    /// <summary>
    /// Время создания события
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Время публикации события в шину
    /// </summary>
    public DateTime? PublishedAt { get; set; }
    
    /// <summary>
    /// Статус обработки
    /// </summary>
    public OutboxMessageStatus Status { get; set; }
    
    /// <summary>
    /// Количество попыток публикации
    /// </summary>
    public int RetryCount { get; set; }
    
    /// <summary>
    /// Последняя ошибка при публикации
    /// </summary>
    public string? LastError { get; set; }
}

public enum OutboxMessageStatus
{
    /// <summary>
    /// Ожидает публикации
    /// </summary>
    Pending = 0,
    
    /// <summary>
    /// Опубликовано в шину
    /// </summary>
    Published = 1,
    
    /// <summary>
    /// Ошибка публикации (требует повторной попытки)
    /// </summary>
    Failed = 2
}
