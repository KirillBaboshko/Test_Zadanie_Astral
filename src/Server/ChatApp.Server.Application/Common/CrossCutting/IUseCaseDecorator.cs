namespace ChatApp.Server.Application.Common.CrossCutting;

/// <summary>
/// Интерфейс для декораторов Use Case
/// Позволяет добавлять Cross-Cutting Concerns (логирование, транзакции и т.д.)
/// </summary>
public interface IUseCaseDecorator<TRequest, TResponse>
{
    /// <summary>
    /// Выполняет декорированный Use Case
    /// </summary>
    Task<TResponse> ExecuteAsync(
        TRequest request,
        Func<TRequest, CancellationToken, Task<TResponse>> next,
        CancellationToken cancellationToken = default);
}
