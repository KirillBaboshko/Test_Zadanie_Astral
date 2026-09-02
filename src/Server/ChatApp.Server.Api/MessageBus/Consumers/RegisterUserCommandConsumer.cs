using ChatApp.Server.Application.Commands.Auth;
using ChatApp.Shared.Messages.Commands;
using ChatApp.Shared.Messages.Responses;
using MassTransit;
using MediatR;

namespace ChatApp.Server.Api.MessageBus.Consumers;

/// <summary>
/// Consumer для обработки команды регистрации пользователя
/// Request-Response паттерн: возвращает токен клиенту
/// </summary>
public class RegisterUserCommandConsumer : IConsumer<RegisterUserCommand>
{
    private readonly IMediator _mediator;
    private readonly ILogger<RegisterUserCommandConsumer> _logger;

    public RegisterUserCommandConsumer(
        IMediator mediator,
        ILogger<RegisterUserCommandConsumer> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Consume(ConsumeContext<RegisterUserCommand> context)
    {
        var rabbitCommand = context.Message;
        
        _logger.LogInformation(
            "[RabbitMQ Command Consumer] Получена команда регистрации: Username={Username}",
            rabbitCommand.Username);

        try
        {
            var command = new RegisterCommand(rabbitCommand.Username, rabbitCommand.Password);
            var response = await _mediator.Send(command, context.CancellationToken);

            if (!response.Success)
            {
                // Пользователь уже существует
                await context.RespondAsync(new RegisterUserResponse
                {
                    Success = false,
                    ErrorMessage = response.ErrorMessage ?? "Не удалось зарегистрировать пользователя"
                });
                
                _logger.LogWarning(
                    "[RabbitMQ Command Consumer] Регистрация не удалась: {ErrorMessage}",
                    response.ErrorMessage);
                return;
            }

            // Успешная регистрация
            await context.RespondAsync(new RegisterUserResponse
            {
                Success = true,
                Token = response.Token!,
                UserId = response.UserId,
                Username = response.Username!
            });

            _logger.LogInformation(
                "[RabbitMQ Command Consumer] Регистрация успешна: {Username} (Id={UserId})",
                response.Username,
                response.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "[RabbitMQ Command Consumer] Ошибка при регистрации пользователя {Username}",
                rabbitCommand.Username);

            await context.RespondAsync(new RegisterUserResponse
            {
                Success = false,
                ErrorMessage = "Внутренняя ошибка сервера"
            });
        }
    }
}
