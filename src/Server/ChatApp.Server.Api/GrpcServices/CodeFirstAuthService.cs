using ChatApp.Server.Application.Commands.Auth;
using ChatApp.Shared.Grpc.Contracts;
using MediatR;
using ProtoBuf.Grpc;

namespace ChatApp.Server.Api.GrpcServices;

/// <summary>
/// Реализация сервиса аутентификации через code-first подход
/// </summary>
public class CodeFirstAuthService : IAuthService
{
    private readonly IMediator _mediator;
    private readonly ILogger<CodeFirstAuthService> _logger;

    public CodeFirstAuthService(
        IMediator mediator,
        ILogger<CodeFirstAuthService> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<AuthResponse> Register(RegisterRequest request, CallContext context = default)
    {
        try
        {
            var serverInfo = $"[Code-First Server]";
            _logger.LogInformation("{ServerInfo} gRPC Register request for username: {Username}", serverInfo, request.Username);

            var command = new RegisterCommand(request.Username, request.Password);
            var response = await _mediator.Send(command, context.CancellationToken);

            if (!response.Success)
            {
                return new AuthResponse
                {
                    Token = string.Empty,
                    Username = string.Empty,
                    ExpiresAt = 0,
                    Error = response.ErrorMessage ?? "Не удалось зарегистрировать пользователя"
                };
            }

            _logger.LogInformation("{ServerInfo} Registration successful for: {Username}", serverInfo, request.Username);

            // Вычисляем ExpiresAt (токен действителен 7 дней)
            var expiresAt = DateTime.UtcNow.AddDays(7);

            return new AuthResponse
            {
                Token = response.Token!,
                Username = response.Username!,
                ExpiresAt = new DateTimeOffset(expiresAt).ToUnixTimeMilliseconds(),
                Error = string.Empty
            };
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Registration failed for username: {Username}", request.Username);

            return new AuthResponse
            {
                Token = string.Empty,
                Username = string.Empty,
                ExpiresAt = 0,
                Error = ex.Message
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during registration");
            throw;
        }
    }

    public async Task<AuthResponse> Login(LoginRequest request, CallContext context = default)
    {
        try
        {
            var serverInfo = $"[Code-First Server]";
            _logger.LogInformation("{ServerInfo} gRPC Login request for username: {Username}", serverInfo, request.Username);

            var command = new LoginCommand(request.Username, request.Password);
            var response = await _mediator.Send(command, context.CancellationToken);

            if (!response.Success)
            {
                return new AuthResponse
                {
                    Token = string.Empty,
                    Username = string.Empty,
                    ExpiresAt = 0,
                    Error = response.ErrorMessage ?? "Неверное имя пользователя или пароль"
                };
            }

            _logger.LogInformation("{ServerInfo} Login successful for: {Username}", serverInfo, request.Username);

            // Вычисляем ExpiresAt (токен действителен 7 дней)
            var expiresAt = DateTime.UtcNow.AddDays(7);

            return new AuthResponse
            {
                Token = response.Token!,
                Username = response.Username!,
                ExpiresAt = new DateTimeOffset(expiresAt).ToUnixTimeMilliseconds(),
                Error = string.Empty
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during login");
            throw;
        }
    }
}
