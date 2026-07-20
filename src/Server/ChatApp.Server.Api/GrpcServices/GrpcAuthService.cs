using ChatApp.Contracts.Requests;
using ChatApp.Server.Api.Grpc;
using ChatApp.Server.Application.UseCases.Auth;
using Grpc.Core;

namespace ChatApp.Server.Api.GrpcServices;

/// <summary>
/// gRPC сервис аутентификации
/// </summary>
public class GrpcAuthService : AuthService.AuthServiceBase
{
    private readonly RegisterUseCase _registerUseCase;
    private readonly LoginUseCase _loginUseCase;
    private readonly ILogger<GrpcAuthService> _logger;

    public GrpcAuthService(
        RegisterUseCase registerUseCase,
        LoginUseCase loginUseCase,
        ILogger<GrpcAuthService> logger)
    {
        _registerUseCase = registerUseCase;
        _loginUseCase = loginUseCase;
        _logger = logger;
    }

    /// <summary>
    /// Регистрация пользователя через gRPC
    /// </summary>
    public override async Task<AuthResponseProto> Register(RegisterRequestProto request, ServerCallContext context)
    {
        try
        {
            _logger.LogDebug("gRPC Register request for username: {Username}", request.Username);

            var contractRequest = new Contracts.Requests.RegisterRequest
            {
                Username = request.Username,
                Password = request.Password
            };

            var authResponse = await _registerUseCase.ExecuteAsync(contractRequest, context.CancellationToken);

            if (authResponse == null)
            {
                return new AuthResponseProto
                {
                    Token = string.Empty,
                    Username = string.Empty,
                    ExpiresAt = 0,
                    Error = "Не удалось зарегистрировать пользователя"
                };
            }

            return new AuthResponseProto
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
            
            return new AuthResponseProto
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
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    /// <summary>
    /// Вход пользователя через gRPC
    /// </summary>
    public override async Task<AuthResponseProto> Login(LoginRequestProto request, ServerCallContext context)
    {
        try
        {
            _logger.LogDebug("gRPC Login request for username: {Username}", request.Username);

            var contractRequest = new Contracts.Requests.LoginRequest
            {
                Username = request.Username,
                Password = request.Password
            };

            var authResponse = await _loginUseCase.ExecuteAsync(contractRequest, context.CancellationToken);

            if (authResponse == null)
            {
                return new AuthResponseProto
                {
                    Token = string.Empty,
                    Username = string.Empty,
                    ExpiresAt = 0,
                    Error = "Неверное имя пользователя или пароль"
                };
            }

            return new AuthResponseProto
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
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }
}
