using ChatApp.Contracts.Requests;
using ChatApp.Server.Application.UseCases.Auth;
using ChatApp.Shared.Messages.Commands;
using ChatApp.Shared.Messages.Responses;
using MassTransit;

namespace ChatApp.Server.Api.MessageBus.Consumers;

/// <summary>
/// Consumer для обработки команды регистрации пользователя
/// Request-Response паттерн: возвращает токен клиенту
/// </summary>
public class RegisterUserCommandConsumer : IConsumer<RegisterUserCommand>
{
    private readonly RegisterUseCase _registerUseCase;
    private readonly ILogger<RegisterUserCommandConsumer> _logger;

    public RegisterUserCommandConsumer(
        RegisterUseCase registerUseCase,
        ILogger<RegisterUserCommandConsumer> logger)
    {
        _registerUseCase = registerUseCase ?? throw new ArgumentNullException(nameof(registerUseCase));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Consume(ConsumeContext<RegisterUserCommand> context)
    {
        var command = context.Message;
        
        _logger.LogInformation(
            "[RabbitMQ Command Consumer] Получена команда регистрации: Username={Username}",
            command.Username);

        try
        {
            var request = new RegisterRequest
            {
                Username = command.Username,
                Password = command.Password
            };

            var authResponse = await _registerUseCase.ExecuteAsync(request, context.CancellationToken);

            if (authResponse == null)
            {
                // Пользователь уже существует
                await context.RespondAsync(new RegisterUserResponse
                {
                    Success = false,
                    ErrorMessage = "Пользователь с таким именем уже существует"
                });
                
                _logger.LogWarning(
                    "[RabbitMQ Command Consumer] Регистрация не удалась: пользователь {Username} уже существует",
                    command.Username);
                return;
            }

            // Успешная регистрация
            await context.RespondAsync(new RegisterUserResponse
            {
                Success = true,
                Token = authResponse.Token,
                UserId = authResponse.UserId,
                Username = authResponse.Username
            });

            _logger.LogInformation(
                "[RabbitMQ Command Consumer] Регистрация успешна: {Username} (Id={UserId})",
                authResponse.Username,
                authResponse.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "[RabbitMQ Command Consumer] Ошибка при регистрации пользователя {Username}",
                command.Username);

            await context.RespondAsync(new RegisterUserResponse
            {
                Success = false,
                ErrorMessage = "Внутренняя ошибка сервера"
            });
        }
    }
}
