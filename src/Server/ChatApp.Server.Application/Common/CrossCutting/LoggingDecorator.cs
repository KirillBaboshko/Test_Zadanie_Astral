using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;

namespace ChatApp.Server.Application.Common.CrossCutting;

/// <summary>
/// Декоратор для логирования выполнения Use Case
/// Автоматически логирует начало, завершение, ошибки и время выполнения
/// </summary>
public class LoggingDecorator<TRequest, TResponse> : IUseCaseDecorator<TRequest, TResponse>
{
    private readonly ILogger<LoggingDecorator<TRequest, TResponse>> _logger;

    public LoggingDecorator(ILogger<LoggingDecorator<TRequest, TResponse>> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<TResponse> ExecuteAsync(
        TRequest request,
        Func<TRequest, CancellationToken, Task<TResponse>> next,
        CancellationToken cancellationToken = default)
    {
        var useCaseName = typeof(TResponse).Name.Replace("Response", "");
        var requestType = typeof(TRequest).Name;

        _logger.LogInformation(
            "[UseCase Start] {UseCase} with request {RequestType}",
            useCaseName,
            requestType);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await next(request, cancellationToken);

            stopwatch.Stop();

            _logger.LogInformation(
                "[UseCase Success] {UseCase} completed in {ElapsedMs}ms",
                useCaseName,
                stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex,
                "[UseCase Error] {UseCase} failed after {ElapsedMs}ms: {ErrorMessage}",
                useCaseName,
                stopwatch.ElapsedMilliseconds,
                ex.Message);

            throw;
        }
    }
}
