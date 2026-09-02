using ChatApp.Server.Application.Commands.Auth;
using ChatApp.Shared.Messages.Commands;
using ChatApp.Shared.Messages.Responses;
using MassTransit;
using MediatR;

namespace ChatApp.Server.Api.MessageBus.Consumers;

/// <summary>
/// Consumer для обработки команды входа в систему
/// Request-Response паттерн: возвращает токен клиенту
/// </summary>
public class LoginUserCommandConsumer : IConsumer<LoginUserCommand>
{
    private readonly IMediator _mediator;
    private readonly ILogger<LoginUserCommandConsumer> _logger;

    public LoginUserCommandConsumer(
        IMediator mediator,
        ILogger<LoginUserCommandConsumer> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Consume(ConsumeContext<LoginUserCommand> context)
    {
        var rabbitCommand = context.Message;
        
        _logger.LogInformation(
            "[RabbitMQ Command Consumer] Получена команда входа: Username={Username}",
            rabbitCommand.Username);

        try
        {
            var command = new LoginCommand(rabbitCommand.Username, rabbitCommand.Password);
            var response = await _mediator.Send(command, context.CancellationToken);

            if (!response.Success)
            {
                await context.RespondAsync(new LoginUserResponse
                {
                    Success = false,
                    ErrorMessage = response.ErrorMessage ?? "Не удалось войти в систему"
                });
                
                _logger.LogWarning(
                    "[RabbitMQ Command Consumer] Вход не удался: {ErrorMessage}",
                    response.ErrorMessage);
                return;
            }
            
            await context.RespondAsync(new LoginUserResponse
            {
                Success = true,
                Token = response.Token!,
                UserId = response.UserId,
                Username = response.Username!
            });

            _logger.LogInformation(
                "[RabbitMQ Command Consumer] Вход успешен: {Username} (Id={UserId})",
                response.Username,
                response.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "[RabbitMQ Command Consumer] Ошибка при входе пользователя {Username}",
                rabbitCommand.Username);

            await context.RespondAsync(new LoginUserResponse
            {
                Success = false,
                ErrorMessage = "Внутренняя ошибка сервера"
            });
        }
    }
}
