using ChatApp.Server.Domain.Abstractions;
using MediatR;

namespace ChatApp.Server.Application.Behaviors;

/// <summary>
/// Pipeline Behavior для управления транзакциями через Unit of Work
/// Автоматически сохраняет изменения после выполнения команды/запроса
/// </summary>
public sealed class UnitOfWorkBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IUnitOfWork _unitOfWork;

    public UnitOfWorkBehavior(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Выполняем следующий Behavior или Handler
        var response = await next();

        // Сохраняем изменения в БД (Unit of Work гарантирует транзакцию)
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return response;
    }
}
