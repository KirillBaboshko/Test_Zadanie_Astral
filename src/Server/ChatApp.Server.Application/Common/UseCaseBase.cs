using ChatApp.Server.Domain.Abstractions;

namespace ChatApp.Server.Application.Common;
public abstract class UseCaseBase
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Инициализирует базовый класс Use Case
    /// </summary>
    /// <param name="unitOfWork">Unit of Work для автоматического сохранения изменений</param>
    protected UseCaseBase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    /// <summary>
    /// Выполняет операцию с автоматическим сохранением изменений через UnitOfWork.
    /// Гарантирует вызов SaveChangesAsync после успешного выполнения операции.
    /// </summary>
    /// <typeparam name="TResult">Тип возвращаемого результата</typeparam>
    /// <param name="operation">Асинхронная операция для выполнения</param>
    /// <param name="cancellationToken">Токен отмены операции</param>
    /// <returns>Результат выполнения операции</returns>
    protected async Task<TResult> ExecuteWithUnitOfWorkAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        var result = await operation(cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return result;
    }

    /// <summary>
    /// Выполняет операцию без возврата значения с автоматическим сохранением изменений через UnitOfWork.
    /// Гарантирует вызов SaveChangesAsync после успешного выполнения операции.
    /// </summary>
    /// <param name="operation">Асинхронная операция для выполнения</param>
    /// <param name="cancellationToken">Токен отмены операции</param>
    protected async Task ExecuteWithUnitOfWorkAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        await operation(cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
