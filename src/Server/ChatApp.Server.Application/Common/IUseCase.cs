namespace ChatApp.Server.Application.Common;

/// <summary>
/// Базовый интерфейс для всех Use Cases
/// Определяет контракт для выполнения бизнес-логики
/// </summary>
/// <typeparam name="TRequest">Тип запроса</typeparam>
/// <typeparam name="TResponse">Тип ответа</typeparam>
public interface IUseCase<TRequest, TResponse>
{
    /// <summary>
    /// Выполняет бизнес-логику Use Case
    /// </summary>
    Task<TResponse> ExecuteAsync(TRequest request, CancellationToken cancellationToken = default);
}
