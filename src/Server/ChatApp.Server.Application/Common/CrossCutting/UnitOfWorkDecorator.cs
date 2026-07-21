using ChatApp.Server.Domain.Abstractions;

namespace ChatApp.Server.Application.Common.CrossCutting;

/// <summary>
/// Декоратор для управления транзакциями через Unit of Work
/// Автоматически сохраняет изменения после выполнения Use Case
/// </summary>
public class UnitOfWorkDecorator<TRequest, TResponse> : IUseCaseDecorator<TRequest, TResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public UnitOfWorkDecorator(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<TResponse> ExecuteAsync(
        TRequest request,
        Func<TRequest, CancellationToken, Task<TResponse>> next,
        CancellationToken cancellationToken = default)
    {
        // Выполняем следующий декоратор или Use Case
        var result = await next(request, cancellationToken);

        // Сохраняем изменения в БД
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return result;
    }
}
