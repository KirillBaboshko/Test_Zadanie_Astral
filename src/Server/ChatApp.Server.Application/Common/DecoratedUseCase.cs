using ChatApp.Server.Application.Common.CrossCutting;

namespace ChatApp.Server.Application.Common;

/// <summary>
/// Базовый класс для Use Case с поддержкой декораторов
/// Автоматически применяет все зарегистрированные декораторы в правильном порядке
/// </summary>
public abstract class DecoratedUseCase<TRequest, TResponse> : IUseCase<TRequest, TResponse>
{
    private readonly IEnumerable<IUseCaseDecorator<TRequest, TResponse>> _decorators;

    protected DecoratedUseCase(IEnumerable<IUseCaseDecorator<TRequest, TResponse>> decorators)
    {
        _decorators = decorators ?? throw new ArgumentNullException(nameof(decorators));
    }

    /// <summary>
    /// Выполняет Use Case с применением всех декораторов
    /// Порядок выполнения: LoggingDecorator -> UnitOfWorkDecorator -> ExecuteCoreAsync
    /// </summary>
    public async Task<TResponse> ExecuteAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        Func<TRequest, CancellationToken, Task<TResponse>> pipeline = ExecuteCoreAsync;

        foreach (var decorator in _decorators.Reverse())
        {
            var currentPipeline = pipeline;
            var currentDecorator = decorator;

            pipeline = (req, ct) => currentDecorator.ExecuteAsync(req, currentPipeline, ct);
        }

        return await pipeline(request, cancellationToken);
    }

    /// <summary>
    /// Основная бизнес-логика Use Case
    /// Переопределяется в конкретных Use Case
    /// </summary>
    protected abstract Task<TResponse> ExecuteCoreAsync(TRequest request, CancellationToken cancellationToken);
}
