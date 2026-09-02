using System.Net.Http.Json;
using System.Net.Http.Headers;
using ChatApp.Client.Application.Services;
using ChatApp.Contracts.Messages;
using ChatApp.Contracts.Requests;
using ChatApp.Contracts.Responses;

namespace ChatApp.Client.Infrastructure.Http;


public sealed class HttpChatApiClient : IChatApiClient, IDisposable
{
    private readonly HttpClient _httpClient;
    private String? _authToken;

    public HttpChatApiClient(String baseUrl)
    {
        if (String.IsNullOrWhiteSpace(baseUrl))
            throw new ArgumentException("Base URL не может быть пустым", nameof(baseUrl));

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };
    }


    /// <summary>
    /// Регистрирует нового пользователя
    /// </summary>
    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/register", request, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Ошибка регистрации: {response.StatusCode}");
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                Console.WriteLine($"Детали: {errorContent}");
                return null;
            }

            var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken: cancellationToken);
            
            if (authResponse != null)
            {
                SetAuthToken(authResponse.Token);
            }
            
            return authResponse;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Ошибка сети при регистрации: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Авторизует пользователя
    /// </summary>
    public async Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/login", request, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Ошибка авторизации: {response.StatusCode}");
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                Console.WriteLine($"Детали: {errorContent}");
                return null;
            }

            var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken: cancellationToken);
            
            if (authResponse != null)
            {
                SetAuthToken(authResponse.Token);
            }
            
            return authResponse;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Ошибка сети при авторизации: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Устанавливает JWT токен для авторизации запросов
    /// </summary>
    public void SetAuthToken(String token)
    {
        _authToken = token;
        _httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", token);
    }

    /// <summary>
    /// Очищает JWT токен
    /// </summary>
    public void ClearAuthToken()
    {
        _authToken = null;
        _httpClient.DefaultRequestHeaders.Authorization = null;
    }



    /// <summary>
    /// Отправляет новое сообщение от авторизованного пользователя
    /// </summary>
    public async Task<ChatMessageDto?> SendMessageAsync(SendMessageAuthRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            if (String.IsNullOrEmpty(_authToken))
            {
                Console.WriteLine("Ошибка: Необходима авторизация для отправки сообщений");
                return null;
            }

            var response = await _httpClient.PostAsJsonAsync("api/chat/messages", request, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Ошибка отправки сообщения: {response.StatusCode}");
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                Console.WriteLine($"Детали: {errorContent}");
                return null;
            }

            return await response.Content.ReadFromJsonAsync<ChatMessageDto>(cancellationToken: cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Ошибка сети: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Получает список сообщений с возможностью фильтрации по времени
    /// </summary>
    public async Task<GetMessagesResponse?> GetMessagesAsync(DateTime? since = null, Int32 limit = 100, CancellationToken cancellationToken = default)
    {
        try
        {
            var queryParams = $"?limit={limit}";
            if (since.HasValue)
                queryParams += $"&since={since.Value:O}";

            var response = await _httpClient.GetAsync($"api/chat/messages{queryParams}", cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<GetMessagesResponse>(cancellationToken: cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Ошибка получения сообщений: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Получает список сообщений конкретного пользователя по имени
    /// </summary>
    public async Task<GetMessagesResponse?> GetMessagesForNameAsync(Int32 limit = 100, String? senderName = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var queryParams = $"?limit={limit}";
            if (!String.IsNullOrEmpty(senderName))
                queryParams += $"&senderName={Uri.EscapeDataString(senderName)}";

            var response = await _httpClient.GetAsync($"api/chat/messages-for-name{queryParams}", cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<GetMessagesResponse>(cancellationToken: cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Ошибка получения сообщений: {ex.Message}");
            return null;
        }
    }


    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
