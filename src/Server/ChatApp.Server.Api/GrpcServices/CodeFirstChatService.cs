using ChatApp.Contracts.Requests;
using ChatApp.Server.Application.UseCases.GetMessages;
using ChatApp.Server.Application.UseCases.GetUsers;
using ChatApp.Server.Application.UseCases.SendMessage;
using ChatApp.Shared.Grpc.Contracts;
using Microsoft.IdentityModel.Tokens;
using ProtoBuf.Grpc;
using System.IdentityModel.Tokens.Jwt;
using System.Runtime.CompilerServices;

using CallContext = ProtoBuf.Grpc.CallContext;

namespace ChatApp.Server.Api.GrpcServices;

/// <summary>
/// Реализация сервиса чата через code-first подход
/// </summary>
public class CodeFirstChatService : IChatService
{
    private readonly SendMessageUseCase _sendMessageUseCase;
    private readonly GetMessagesUseCase _getMessagesUseCase;
    private readonly GetUsersUseCase _getUsersUseCase;
    private readonly RsaSecurityKey _rsaKey;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CodeFirstChatService> _logger;

    public CodeFirstChatService(
        SendMessageUseCase sendMessageUseCase,
        GetMessagesUseCase getMessagesUseCase,
        GetUsersUseCase getUsersUseCase,
        RsaSecurityKey rsaKey,
        IConfiguration configuration,
        ILogger<CodeFirstChatService> logger)
    {
        _sendMessageUseCase = sendMessageUseCase;
        _getMessagesUseCase = getMessagesUseCase;
        _getUsersUseCase = getUsersUseCase;
        _rsaKey = rsaKey;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<MessageResponse> SendMessage(SendMessageRequest request, CallContext context = default)
    {
        try
        {
            var serverInfo = "[Code-First Server]";
            var userId = ValidateTokenAndGetUserId(request.Token);
            if (userId == null)
            {
                throw new UnauthorizedAccessException("Invalid or expired token");
            }

            _logger.LogInformation("{ServerInfo} gRPC SendMessage request from user: {UserId}", serverInfo, userId);

            var contractRequest = new SendMessageAuthRequest
            {
                Content = request.Content
            };

            var message = await _sendMessageUseCase.ExecuteAuthAsync(userId.Value, contractRequest, context.CancellationToken);

            if (message == null)
            {
                throw new InvalidOperationException("User not found");
            }

            return new MessageResponse
            {
                Id = message.Id.ToString(),
                SenderName = message.SenderName,
                Content = message.Content,
                Timestamp = new DateTimeOffset(message.Timestamp).ToUnixTimeMilliseconds()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during SendMessage");
            throw;
        }
    }

    public async Task<MessagesListResponse> GetMessages(GetMessagesRequest request, CallContext context = default)
    {
        try
        {
            _logger.LogDebug("gRPC GetMessages request");

            DateTime? since = null;
            if (request.SinceTimestamp > 0)
            {
                since = DateTimeOffset.FromUnixTimeMilliseconds(request.SinceTimestamp).UtcDateTime;
            }

            var limit = request.Limit > 0 ? request.Limit : 100;

            var response = await _getMessagesUseCase.ExecuteAsync(since, limit, context.CancellationToken);

            var grpcResponse = new MessagesListResponse
            {
                TotalCount = response.TotalCount,
                Messages = response.Messages.Select(m => new MessageResponse
                {
                    Id = m.Id.ToString(),
                    SenderName = m.SenderName,
                    Content = m.Content,
                    Timestamp = new DateTimeOffset(m.Timestamp).ToUnixTimeMilliseconds()
                }).ToList()
            };

            return grpcResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during GetMessages");
            throw;
        }
    }

    public async Task<MessagesListResponse> GetMessagesByUser(GetMessagesByUserRequest request, CallContext context = default)
    {
        try
        {
            _logger.LogDebug("gRPC GetMessagesByUser request for: {Username}", request.Username);

            var limit = request.Limit > 0 ? request.Limit : 100;

            var response = await _getMessagesUseCase.ExecuteForUsernameAsync(request.Username, limit, context.CancellationToken);

            if (response == null)
            {
                throw new InvalidOperationException("User not found");
            }

            var grpcResponse = new MessagesListResponse
            {
                TotalCount = response.TotalCount,
                Messages = response.Messages.Select(m => new MessageResponse
                {
                    Id = m.Id.ToString(),
                    SenderName = m.SenderName,
                    Content = m.Content,
                    Timestamp = new DateTimeOffset(m.Timestamp).ToUnixTimeMilliseconds()
                }).ToList()
            };

            return grpcResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during GetMessagesByUser");
            throw;
        }
    }

    public async Task<UsersListResponse> GetUsers(GetUsersRequest request, CallContext context = default)
    {
        try
        {
            _logger.LogDebug("gRPC GetUsers request");

            var users = await _getUsersUseCase.ExecuteAsync(context.CancellationToken);

            var grpcResponse = new UsersListResponse
            {
                Users = users.Select(u => new UserInfo
                {
                    Id = u.Id.ToString(),
                    Username = u.Username
                }).ToList()
            };

            return grpcResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during GetUsers");
            throw;
        }
    }

    public async IAsyncEnumerable<MessageResponse> StreamMessages(
        StreamMessagesRequest request,
        CallContext context = default)
    {
        // Валидация токена
        var userId = ValidateTokenAndGetUserId(request.Token);
        if (userId == null)
        {
            throw new UnauthorizedAccessException("Invalid or expired token");
        }

        _logger.LogDebug("gRPC StreamMessages started for user: {UserId}", userId);

        var lastTimestamp = request.SinceTimestamp > 0
            ? DateTimeOffset.FromUnixTimeMilliseconds(request.SinceTimestamp).UtcDateTime
            : DateTime.UtcNow;

        while (!context.CancellationToken.IsCancellationRequested)
        {
            // Получаем новые сообщения
            var response = await _getMessagesUseCase.ExecuteAsync(lastTimestamp, 100, context.CancellationToken);

            // Отправляем новые сообщения клиенту
            foreach (var msg in response.Messages)
            {
                yield return new MessageResponse
                {
                    Id = msg.Id.ToString(),
                    SenderName = msg.SenderName,
                    Content = msg.Content,
                    Timestamp = new DateTimeOffset(msg.Timestamp).ToUnixTimeMilliseconds()
                };

                lastTimestamp = msg.Timestamp;
            }

            // Ждём перед следующей проверкой
            await Task.Delay(TimeSpan.FromSeconds(2), context.CancellationToken);
        }

        _logger.LogDebug("gRPC StreamMessages stopped for user: {UserId}", userId);
    }

    /// <summary>
    /// Валидация JWT токена и получение UserId
    /// </summary>
    private Guid? ValidateTokenAndGetUserId(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = _configuration["Jwt:Audience"],
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = _rsaKey,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
            var userIdClaim = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return null;
            }

            return userId;
        }
        catch
        {
            return null;
        }
    }
}
