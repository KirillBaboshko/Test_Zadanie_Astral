using System.Net.Http.Headers;
using System.Net.Http.Json;
using ChatApp.Contracts.Messages;
using ChatApp.Contracts.Requests;
using ChatApp.Contracts.Responses;

namespace ChatApp.Client.Blazor.Services;

/// <summary>
/// Сервис для работы с чатом через REST API
/// </summary>
public class ChatService
{
    private readonly HttpClient _httpClient;
    private readonly AuthService _authService;

    public ChatService(HttpClient httpClient, AuthService authService)
    {
        _httpClient = httpClient;
        _authService = authService;
    }

    /// <summary>
    /// Получение всех сообщений
    /// </summary>
    public async Task<List<ChatMessageDto>> GetMessagesAsync(DateTime? since = null, int limit = 100)
    {
        try
        {
            var query = $"/api/chat/messages?limit={limit}";
            if (since.HasValue)
            {
                query += $"&since={since.Value:O}";
            }

            var response = await _httpClient.GetFromJsonAsync<GetMessagesResponse>(query);
            return response?.Messages ?? new List<ChatMessageDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка получения сообщений: {ex.Message}");
            return new List<ChatMessageDto>();
        }
    }

    /// <summary>
    /// Отправка сообщения (требуется авторизация)
    /// </summary>
    public async Task<(bool Success, string Message)> SendMessageAsync(string content)
    {
        try
        {
            if (!_authService.IsAuthenticated)
            {
                return (false, "Необходима авторизация");
            }

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/chat/messages");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _authService.Token);
            
            var requestBody = new SendMessageAuthRequest { Content = content };
            var jsonBody = System.Text.Json.JsonSerializer.Serialize(requestBody);
            httpRequest.Content = new StringContent(jsonBody, System.Text.Encoding.UTF8, "application/json");
            
            var response = await _httpClient.SendAsync(httpRequest);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return (false, $"Ошибка отправки: {error}");
            }

            return (true, "Сообщение отправлено");
        }
        catch (Exception ex)
        {
            return (false, $"Ошибка: {ex.Message}");
        }
    }
}
