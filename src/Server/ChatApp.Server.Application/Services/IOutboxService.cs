namespace ChatApp.Server.Application.Services;

/// <summary>
/// Сервис для работы с Outbox паттерном
/// Сохраняет события в БД для последующей публикации в Message Bus
/// </summary>
public interface IOutboxService
{
    /// <summary>
    /// Добавляет событие в Outbox для публикации
    /// Вызывается внутри транзакции вместе с основными данными
    /// </summary>
    Task AddEventAsync<TEvent>(TEvent eventData, CancellationToken cancellationToken = default) 
        where TEvent : class;
}
