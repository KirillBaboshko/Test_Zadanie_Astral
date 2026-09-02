using ChatApp.Client.Application.Services;
using ChatApp.Contracts.Messages;
using ChatApp.Contracts.Requests;
using ChatApp.Contracts.Responses;
using ChatApp.Shared.Grpc.Contracts;
using Grpc.Net.Client;
using ProtoBuf.Grpc.Client;

namespace ChatApp.Client.Infrastructure.Grpc;


public class CodeFirstGrpcChatApiClient : IChatApiClient
{
    private readonly GrpcChannel _channel;
    private readonly IAuthService _authClient;
    private readonly IChatService _chatClient;
    private readonly String _baseUrl;
    private String? _authToken;

    public CodeFirstGrpcChatApiClient(String baseUrl)
    {
        _baseUrl = baseUrl;
        const Int32 delay = 60;
        const Int32 timeout = 30;
        
        var channelOptions = new GrpcChannelOptions
        {
            HttpHandler = new SocketsHttpHandler
            {
                PooledConnectionIdleTimeout = Timeout.InfiniteTimeSpan,
                KeepAlivePingDelay = TimeSpan.FromSeconds(delay),
                KeepAlivePingTimeout = TimeSpan.FromSeconds(timeout),
                EnableMultipleHttp2Connections = true
            }
        };

        _channel = GrpcChannel.ForAddress(baseUrl, channelOptions);
        
        _authClient = _channel.CreateGrpcService<IAuthService>();
        _chatClient = _channel.CreateGrpcService<IChatService>();
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
    /// Регистрация через code-first gRPC
    /// </summary>
    public async Task<Contracts.Responses.AuthResponse?> RegisterAsync(Contracts.Requests.RegisterRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var grpcRequest = new Shared.Grpc.Contracts.RegisterRequest
            {
                Username = request.Username,
                Password = request.Password
            };

            var response = await _authClient.Register(grpcRequest);

            if (!string.IsNullOrEmpty(response.Error))
            {
                Console.WriteLine($"Ошибка регистрации: {response.Error}");
                return null;
            }
            
            SetAuthToken(response.Token);

            return new Contracts.Responses.AuthResponse
            {
                Token = response.Token,
                Username = response.Username,
                ExpiresAt = DateTimeOffset.FromUnixTimeMilliseconds(response.ExpiresAt).UtcDateTime
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при регистрации: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Вход через code-first gRPC
    /// </summary>
    public async Task<Contracts.Responses.AuthResponse?> LoginAsync(Contracts.Requests.LoginRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var grpcRequest = new Shared.Grpc.Contracts.LoginRequest
            {
                Username = request.Username,
                Password = request.Password
            };

            var response = await _authClient.Login(grpcRequest);

            if (!string.IsNullOrEmpty(response.Error))
            {
                Console.WriteLine($"Ошибка входа: {response.Error}");
                return null;
            }

            SetAuthToken(response.Token);

            return new Contracts.Responses.AuthResponse
            {
                Token = response.Token,
                Username = response.Username,
                ExpiresAt = DateTimeOffset.FromUnixTimeMilliseconds(response.ExpiresAt).UtcDateTime
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при входе: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Отправка сообщения через code-first gRPC
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

            var grpcRequest = new Shared.Grpc.Contracts.SendMessageRequest
            {
                Token = _authToken,
                Content = request.Content
            };

            var response = await _chatClient.SendMessage(grpcRequest);

            return new ChatMessageDto
            {
                Id = Guid.Parse(response.Id),
                SenderName = response.SenderName,
                Content = response.Content,
                Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(response.Timestamp).UtcDateTime
            };
        }
        catch (UnauthorizedAccessException)
        {
            Console.WriteLine("Токен истёк или невалиден");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при отправке сообщения: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Получение сообщений через code-first gRPC
    /// </summary>
    public async Task<GetMessagesResponse?> GetMessagesAsync(DateTime? since = null, Int32 limit = 100, CancellationToken cancellationToken = default)
    {
        try
        {
            var grpcRequest = new Shared.Grpc.Contracts.GetMessagesRequest
            {
                SinceTimestamp = since.HasValue 
                    ? new DateTimeOffset(since.Value).ToUnixTimeMilliseconds() 
                    : 0,
                Limit = limit
            };

            var response = await _chatClient.GetMessages(grpcRequest);

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
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при получении сообщений: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Получение сообщений пользователя через code-first gRPC
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

            var grpcRequest = new Shared.Grpc.Contracts.GetMessagesByUserRequest
            {
                Username = senderName,
                Limit = limit
            };

            var response = await _chatClient.GetMessagesByUser(grpcRequest);

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
        catch (InvalidOperationException)
        {
            Console.WriteLine($"Пользователь {senderName} не найден");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Подписка на стрим новых сообщений через code-first gRPC Server Streaming
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

            var request = new Shared.Grpc.Contracts.StreamMessagesRequest
            {
                Token = _authToken,
                SinceTimestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds()
            };

            var stream = _chatClient.StreamMessages(request);

            await foreach (var message in stream.WithCancellation(cancellationToken))
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
        catch (OperationCanceledException)
        {
            Console.WriteLine("Стрим сообщений отменён");
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
