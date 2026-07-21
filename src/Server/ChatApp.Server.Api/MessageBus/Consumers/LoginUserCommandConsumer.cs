using ChatApp.Contracts.Requests;
using ChatApp.Server.Application.UseCases.Auth;
using ChatApp.Shared.Messages.Commands;
using ChatApp.Shared.Messages.Responses;
using MassTransit;

namespace ChatApp.Server.Api.MessageBus.Consumers;

/// <summary>
/// Consumer для обработки команды входа в систему
/// Request-Response паттерн: возвращает токен клиенту
/// </summary>
public class LoginUserCommandConsumer : IConsumer<LoginUserCommand>
{
    private readonly LoginUseCase _loginUseCase;
    private readonly ILogger<LoginUserCommandConsumer> _logger;

    public LoginUserCommandConsumer(
        LoginUseCase loginUseCase,
        ILogger<LoginUserCommandConsumer> logger)
    {
        _loginUseCase = loginUseCase ?? throw new ArgumentNullException(nameof(loginUseCase));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Consume(ConsumeContext<LoginUserCommand> context)
    {
        var command = context.Message;
        
        _logger.LogInformation(
            "[RabbitMQ Command Consumer] Получена команда входа: Username={Username}",
            command.Username);

        try
        {
            var request = new LoginRequest
            {
                Username = command.Username,
                Password = command.Password
            };

            var authResponse = await _loginUseCase.ExecuteAsync(request, context.CancellationToken);

            if (authResponse == null)
            {
                await context.RespondAsync(new LoginUserResponse
                {
                    Success = false,
                    ErrorMessage = "Неверное имя пользователя или пароль"
                });
                
                _logger.LogWarning(
                    "[RabbitMQ Command Consumer] Вход не удался: неверные учетные данные для {Username}",
                    command.Username);
                return;
            }
            await context.RespondAsync(new LoginUserResponse
            {
                Success = true,
                Token = authResponse.Token,
                UserId = authResponse.UserId,
                Username = authResponse.Username
            });

            _logger.LogInformation(
                "[RabbitMQ Command Consumer] Вход успешен: {Username} (Id={UserId})",
                authResponse.Username,
                authResponse.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "[RabbitMQ Command Consumer] Ошибка при входе пользователя {Username}",
                command.Username);

            await context.RespondAsync(new LoginUserResponse
            {
                Success = false,
                ErrorMessage = "Внутренняя ошибка сервера"
            });
        }
    }
}
