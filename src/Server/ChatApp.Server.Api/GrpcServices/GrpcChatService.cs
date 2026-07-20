using ChatApp.Contracts.Requests;
using ChatApp.Server.Api.Grpc;
using ChatApp.Server.Application.UseCases.GetMessages;
using ChatApp.Server.Application.UseCases.GetUsers;
using ChatApp.Server.Application.UseCases.SendMessage;
using Grpc.Core;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace ChatApp.Server.Api.GrpcServices;

/// <summary>
/// gRPC сервис для работы с чатом
/// </summary>
public class GrpcChatService : ChatService.ChatServiceBase
{
    private readonly SendMessageUseCase _sendMessageUseCase;
    private readonly GetMessagesUseCase _getMessagesUseCase;
    private readonly GetUsersUseCase _getUsersUseCase;
    private readonly RsaSecurityKey _rsaKey;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GrpcChatService> _logger;

    public GrpcChatService(
        SendMessageUseCase sendMessageUseCase,
        GetMessagesUseCase getMessagesUseCase,
        GetUsersUseCase getUsersUseCase,
        RsaSecurityKey rsaKey,
        IConfiguration configuration,
        ILogger<GrpcChatService> logger)
    {
        _sendMessageUseCase = sendMessageUseCase;
        _getMessagesUseCase = getMessagesUseCase;
        _getUsersUseCase = getUsersUseCase;
        _rsaKey = rsaKey;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Отправка сообщения через gRPC
    /// </summary>
    public override async Task<MessageResponse> SendMessage(SendMessageRequest request, ServerCallContext context)
    {
        try
        {
            // Валидация и парсинг токена
            var userId = ValidateTokenAndGetUserId(request.Token);
            if (userId == null)
            {
                throw new RpcException(new Status(StatusCode.Unauthenticated, "Invalid or expired token"));
            }

            _logger.LogDebug("gRPC SendMessage request from user: {UserId}", userId);

            var contractRequest = new SendMessageAuthRequest
            {
                Content = request.Content
            };

            var message = await _sendMessageUseCase.ExecuteAuthAsync(userId.Value, contractRequest, context.CancellationToken);

            if (message == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "User not found"));
            }

            return new MessageResponse
            {
                Id = message.Id.ToString(),
                SenderName = message.SenderName,
                Content = message.Content,
                Timestamp = new DateTimeOffset(message.Timestamp).ToUnixTimeMilliseconds()
            };
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during SendMessage");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    /// <summary>
    /// Получение сообщений через gRPC
    /// </summary>
    public override async Task<MessagesListResponse> GetMessages(GetMessagesRequest request, ServerCallContext context)
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
                TotalCount = response.TotalCount
            };

            foreach (var msg in response.Messages)
            {
                grpcResponse.Messages.Add(new MessageResponse
                {
                    Id = msg.Id.ToString(),
                    SenderName = msg.SenderName,
                    Content = msg.Content,
                    Timestamp = new DateTimeOffset(msg.Timestamp).ToUnixTimeMilliseconds()
                });
            }

            return grpcResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during GetMessages");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    /// <summary>
    /// Получение сообщений пользователя через gRPC
    /// </summary>
    public override async Task<MessagesListResponse> GetMessagesByUser(GetMessagesByUserRequest request, ServerCallContext context)
    {
        try
        {
            _logger.LogDebug("gRPC GetMessagesByUser request for: {Username}", request.Username);

            var limit = request.Limit > 0 ? request.Limit : 100;

            var response = await _getMessagesUseCase.ExecuteForUsernameAsync(request.Username, limit, context.CancellationToken);

            if (response == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "User not found"));
            }

            var grpcResponse = new MessagesListResponse
            {
                TotalCount = response.TotalCount
            };

            foreach (var msg in response.Messages)
            {
                grpcResponse.Messages.Add(new MessageResponse
                {
                    Id = msg.Id.ToString(),
                    SenderName = msg.SenderName,
                    Content = msg.Content,
                    Timestamp = new DateTimeOffset(msg.Timestamp).ToUnixTimeMilliseconds()
                });
            }

            return grpcResponse;
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during GetMessagesByUser");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    /// <summary>
    /// Получение списка пользователей через gRPC
    /// </summary>
    public override async Task<UsersListResponse> GetUsers(GetUsersRequest request, ServerCallContext context)
    {
        try
        {
            _logger.LogDebug("gRPC GetUsers request");

            var users = await _getUsersUseCase.ExecuteAsync(context.CancellationToken);

            var grpcResponse = new UsersListResponse();

            foreach (var user in users)
            {
                grpcResponse.Users.Add(new UserInfo
                {
                    Id = user.Id.ToString(),
                    Username = user.Username
                });
            }

            return grpcResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during GetUsers");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    /// <summary>
    /// Server-side streaming сообщений
    /// </summary>
    public override async Task StreamMessages(StreamMessagesRequest request, IServerStreamWriter<MessageResponse> responseStream, ServerCallContext context)
    {
        try
        {
            // Валидация токена
            var userId = ValidateTokenAndGetUserId(request.Token);
            if (userId == null)
            {
                throw new RpcException(new Status(StatusCode.Unauthenticated, "Invalid or expired token"));
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
                    await responseStream.WriteAsync(new MessageResponse
                    {
                        Id = msg.Id.ToString(),
                        SenderName = msg.SenderName,
                        Content = msg.Content,
                        Timestamp = new DateTimeOffset(msg.Timestamp).ToUnixTimeMilliseconds()
                    });

                    lastTimestamp = msg.Timestamp;
                }

                // Ждём перед следующей проверкой
                await Task.Delay(TimeSpan.FromSeconds(2), context.CancellationToken);
            }

            _logger.LogDebug("gRPC StreamMessages stopped for user: {UserId}", userId);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Stream cancelled by client");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during StreamMessages");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
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
