using System.Net.Http.Json;
using ChatApp.Client.Application.Services;
using ChatApp.Contracts.Messages;
using ChatApp.Contracts.Requests;
using ChatApp.Contracts.Responses;

namespace ChatApp.Client.Infrastructure.Http;

public sealed class HttpChatApiClient : IChatApiClient, IDisposable
{
    private readonly HttpClient _httpClient;

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

    public async Task<ChatMessageDto?> SendMessageAsync(SendMessageRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/chat/messages", request, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ChatMessageDto>(cancellationToken: cancellationToken);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

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
        catch (HttpRequestException)
        {
            return null;
        }
    }
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
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
