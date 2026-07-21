using ChatApp.Server.Application.UseCases.Auth;
using ChatApp.Shared.Grpc.Contracts;
using ProtoBuf.Grpc;

namespace ChatApp.Server.Api.GrpcServices;

/// <summary>
/// Реализация сервиса аутентификации через code-first подход
/// </summary>
public class CodeFirstAuthService : IAuthService
{
    private readonly RegisterUseCase _registerUseCase;
    private readonly LoginUseCase _loginUseCase;
    private readonly ILogger<CodeFirstAuthService> _logger;

    public CodeFirstAuthService(
        RegisterUseCase registerUseCase,
        LoginUseCase loginUseCase,
        ILogger<CodeFirstAuthService> logger)
    {
        _registerUseCase = registerUseCase;
        _loginUseCase = loginUseCase;
        _logger = logger;
    }

    public async Task<AuthResponse> Register(RegisterRequest request, CallContext context = default)
    {
        try
        {
            var serverInfo = $"[Code-First Server]";
            _logger.LogInformation("{ServerInfo} gRPC Register request for username: {Username}", serverInfo, request.Username);

            var contractRequest = new Contracts.Requests.RegisterRequest
            {
                Username = request.Username,
                Password = request.Password
            };

            var authResponse = await _registerUseCase.ExecuteAsync(contractRequest, context.CancellationToken);

            if (authResponse == null)
            {
                return new AuthResponse
                {
                    Token = string.Empty,
                    Username = string.Empty,
                    ExpiresAt = 0,
                    Error = "Не удалось зарегистрировать пользователя"
                };
            }

            _logger.LogInformation("{ServerInfo} Registration successful for: {Username}", serverInfo, request.Username);

            return new AuthResponse
            {
                Token = authResponse.Token,
                Username = authResponse.Username,
                ExpiresAt = new DateTimeOffset(authResponse.ExpiresAt).ToUnixTimeMilliseconds(),
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

            var contractRequest = new Contracts.Requests.LoginRequest
            {
                Username = request.Username,
                Password = request.Password
            };

            var authResponse = await _loginUseCase.ExecuteAsync(contractRequest, context.CancellationToken);

            if (authResponse == null)
            {
                return new AuthResponse
                {
                    Token = string.Empty,
                    Username = string.Empty,
                    ExpiresAt = 0,
                    Error = "Неверное имя пользователя или пароль"
                };
            }

            _logger.LogInformation("{ServerInfo} Login successful for: {Username}", serverInfo, request.Username);

            return new AuthResponse
            {
                Token = authResponse.Token,
                Username = authResponse.Username,
                ExpiresAt = new DateTimeOffset(authResponse.ExpiresAt).ToUnixTimeMilliseconds(),
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
