using ChatApp.Client.Application.Services;
using ChatApp.Contracts.Messages;
using ChatApp.Contracts.Requests;
using ChatApp.Contracts.Responses;
using ChatApp.Server.Api.Grpc;
using Grpc.Core;
using Grpc.Net.Client;

namespace ChatApp.Client.Infrastructure.Grpc;

/// <summary>
/// gRPC клиент для работы с чатом с поддержкой DNS round-robin
/// </summary>
public class GrpcChatApiClient : IChatApiClient
{
    private readonly GrpcChannel _channel;
    private readonly AuthService.AuthServiceClient _authClient;
    private readonly ChatService.ChatServiceClient _chatClient;
    private readonly String _baseUrl;
    private String? _authToken;

    public GrpcChatApiClient(String baseUrl)
    {
        _baseUrl = baseUrl;

        // Настраиваем gRPC канал с DNS round-robin балансировкой
        var channelOptions = new GrpcChannelOptions
        {
            // HTTP/2 keep-alive для поддержания соединения
            HttpHandler = new SocketsHttpHandler
            {
                PooledConnectionIdleTimeout = Timeout.InfiniteTimeSpan,
                KeepAlivePingDelay = TimeSpan.FromSeconds(60),
                KeepAlivePingTimeout = TimeSpan.FromSeconds(30),
                EnableMultipleHttp2Connections = true
            }
        };

        _channel = GrpcChannel.ForAddress(baseUrl, channelOptions);
        _authClient = new AuthService.AuthServiceClient(_channel);
        _chatClient = new ChatService.ChatServiceClient(_channel);
    }

    /// <summary>
    /// Установка токена для аутентификации
    /// </summary>
    public void SetAuthToken(String token)
    {
        _authToken = token;
    }

    /// <summary>
    /// Очистка токена
    /// </summary>
    public void ClearAuthToken()
    {
        _authToken = null;
    }

    /// <summary>
    /// Регистрация через gRPC
    /// </summary>
    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var grpcRequest = new RegisterRequestProto
            {
                Username = request.Username,
                Password = request.Password
            };

            var response = await _authClient.RegisterAsync(grpcRequest, cancellationToken: cancellationToken);

            if (!string.IsNullOrEmpty(response.Error))
            {
                Console.WriteLine($"Ошибка регистрации: {response.Error}");
                return null;
            }

            // Устанавливаем токен для последующих запросов
            SetAuthToken(response.Token);

            return new AuthResponse
            {
                Token = response.Token,
                Username = response.Username,
                ExpiresAt = DateTimeOffset.FromUnixTimeMilliseconds(response.ExpiresAt).UtcDateTime
            };
        }
        catch (RpcException ex)
        {
            Console.WriteLine($"gRPC ошибка при регистрации: {ex.Status.Detail}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при регистрации: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Вход через gRPC
    /// </summary>
    public async Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var grpcRequest = new LoginRequestProto
            {
                Username = request.Username,
                Password = request.Password
            };

            var response = await _authClient.LoginAsync(grpcRequest, cancellationToken: cancellationToken);

            if (!string.IsNullOrEmpty(response.Error))
            {
                Console.WriteLine($"Ошибка входа: {response.Error}");
                return null;
            }

            // Устанавливаем токен для последующих запросов
            SetAuthToken(response.Token);

            return new AuthResponse
            {
                Token = response.Token,
                Username = response.Username,
                ExpiresAt = DateTimeOffset.FromUnixTimeMilliseconds(response.ExpiresAt).UtcDateTime
            };
        }
        catch (RpcException ex)
        {
            Console.WriteLine($"gRPC ошибка при входе: {ex.Status.Detail}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при входе: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Отправка сообщения через gRPC
    /// </summary>
    public async Task<ChatMessageDto?> SendMessageAsync(SendMessageAuthRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(_authToken))
            {
                Console.WriteLine("Токен не установлен");
                return null;
            }

            var grpcRequest = new Server.Api.Grpc.SendMessageRequest
            {
                Token = _authToken,
                Content = request.Content
            };

            var response = await _chatClient.SendMessageAsync(grpcRequest, cancellationToken: cancellationToken);

            return new ChatMessageDto
            {
                Id = Guid.Parse(response.Id),
                SenderName = response.SenderName,
                Content = response.Content,
                Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(response.Timestamp).UtcDateTime
            };
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Unauthenticated)
        {
            Console.WriteLine("Токен истёк или невалиден");
            return null;
        }
        catch (RpcException ex)
        {
            Console.WriteLine($"gRPC ошибка при отправке сообщения: {ex.Status.Detail}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при отправке сообщения: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Получение сообщений через gRPC
    /// </summary>
    public async Task<GetMessagesResponse?> GetMessagesAsync(DateTime? since = null, Int32 limit = 100, CancellationToken cancellationToken = default)
    {
        try
        {
            var grpcRequest = new Server.Api.Grpc.GetMessagesRequest
            {
                SinceTimestamp = since.HasValue 
                    ? new DateTimeOffset(since.Value).ToUnixTimeMilliseconds() 
                    : 0,
                Limit = limit
            };

            var response = await _chatClient.GetMessagesAsync(grpcRequest, cancellationToken: cancellationToken);

            var messages = response.Messages.Select(m => new ChatMessageDto
            {
                Id = Guid.Parse(m.Id),
                SenderName = m.SenderName,
                Content = m.Content,
                Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(m.Timestamp).UtcDateTime
            }).ToList();

            return new GetMessagesResponse
            {
                Messages = messages,
                TotalCount = response.TotalCount
            };
        }
        catch (RpcException ex)
        {
            Console.WriteLine($"gRPC ошибка при получении сообщений: {ex.Status.Detail}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при получении сообщений: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Получение сообщений пользователя через gRPC
    /// </summary>
    public async Task<GetMessagesResponse?> GetMessagesForNameAsync(Int32 limit = 100, String? senderName = null, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(senderName))
            {
                Console.WriteLine("Имя пользователя не указано");
                return null;
            }

            var grpcRequest = new Server.Api.Grpc.GetMessagesByUserRequest
            {
                Username = senderName,
                Limit = limit
            };

            var response = await _chatClient.GetMessagesByUserAsync(grpcRequest, cancellationToken: cancellationToken);

            var messages = response.Messages.Select(m => new ChatMessageDto
            {
                Id = Guid.Parse(m.Id),
                SenderName = m.SenderName,
                Content = m.Content,
                Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(m.Timestamp).UtcDateTime
            }).ToList();

            return new GetMessagesResponse
            {
                Messages = messages,
                TotalCount = response.TotalCount
            };
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            Console.WriteLine($"Пользователь {senderName} не найден");
            return null;
        }
        catch (RpcException ex)
        {
            Console.WriteLine($"gRPC ошибка: {ex.Status.Detail}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Подписка на стрим новых сообщений через gRPC Server Streaming
    /// </summary>
    public async Task StreamMessagesAsync(Action<ChatMessageDto> onNewMessage, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrEmpty(_authToken))
            {
                Console.WriteLine("Токен не установлен");
                return;
            }

            var request = new Server.Api.Grpc.StreamMessagesRequest
            {
                Token = _authToken,
                SinceTimestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds()
            };

            using var streamingCall = _chatClient.StreamMessages(request, cancellationToken: cancellationToken);

            await foreach (var message in streamingCall.ResponseStream.ReadAllAsync(cancellationToken))
            {
                var dto = new ChatMessageDto
                {
                    Id = Guid.Parse(message.Id),
                    SenderName = message.SenderName,
                    Content = message.Content,
                    Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(message.Timestamp).UtcDateTime
                };

                onNewMessage(dto);
            }
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
        {
            Console.WriteLine("Стрим сообщений отменён");
        }
        catch (RpcException ex)
        {
            Console.WriteLine($"gRPC ошибка стриминга: {ex.Status.Detail}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка стриминга: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _channel?.Dispose();
    }
}

