using System.Net.Http.Json;
using ChatApp.Contracts.Requests;
using ChatApp.Contracts.Responses;
using Microsoft.JSInterop;

namespace ChatApp.Client.Blazor.Services;

/// <summary>
/// Сервис авторизации и регистрации
/// </summary>
public class AuthService
{
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _jsRuntime;

    public AuthService(HttpClient httpClient, IJSRuntime jsRuntime)
    {
        _httpClient = httpClient;
        _jsRuntime = jsRuntime;
    }

    public string? Token { get; private set; }
    public string? Username { get; private set; }
    public Guid? UserId { get; private set; }
    public bool IsAuthenticated => !string.IsNullOrEmpty(Token);

    public event Action? OnAuthStateChanged;

    /// <summary>
    /// Регистрация нового пользователя
    /// </summary>
    public async Task<(bool Success, string Message)> RegisterAsync(string username, string password)
    {
        try
        {
            var request = new RegisterRequest { Username = username, Password = password };
            var response = await _httpClient.PostAsJsonAsync("/api/auth/register", request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return (false, $"Ошибка регистрации: {error}");
            }

            var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
            if (authResponse != null)
            {
                await SetAuthDataAsync(authResponse);
                return (true, "Регистрация успешна!");
            }

            return (false, "Ошибка обработки ответа сервера");
        }
        catch (Exception ex)
        {
            return (false, $"Ошибка: {ex.Message}");
        }
    }

    /// <summary>
    /// Вход существующего пользователя
    /// </summary>
    public async Task<(bool Success, string Message)> LoginAsync(string username, string password)
    {
        try
        {
            var request = new LoginRequest { Username = username, Password = password };
            var response = await _httpClient.PostAsJsonAsync("/api/auth/login", request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return (false, $"Ошибка входа: {error}");
            }

            var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
            if (authResponse != null)
            {
                await SetAuthDataAsync(authResponse);
                return (true, "Вход выполнен!");
            }

            return (false, "Ошибка обработки ответа сервера");
        }
        catch (Exception ex)
        {
            return (false, $"Ошибка: {ex.Message}");
        }
    }

    /// <summary>
    /// Выход из системы
    /// </summary>
    public async Task LogoutAsync()
    {
        Token = null;
        Username = null;
        UserId = null;

        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "authToken");
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "username");
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "userId");

        OnAuthStateChanged?.Invoke();
    }

    /// <summary>
    /// Восстановление сессии из localStorage
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            Token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "authToken");
            Username = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "username");
            var userIdStr = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "userId");

            if (!string.IsNullOrEmpty(userIdStr) && Guid.TryParse(userIdStr, out var userId))
            {
                UserId = userId;
            }

            if (IsAuthenticated)
            {
                OnAuthStateChanged?.Invoke();
            }
        }
        catch
        {
            // Игнорируем ошибки при инициализации
        }
    }

    /// <summary>
    /// Сохранение данных авторизации
    /// </summary>
    private async Task SetAuthDataAsync(AuthResponse authResponse)
    {
        Token = authResponse.Token;
        Username = authResponse.Username;
        UserId = authResponse.UserId;

        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "authToken", Token);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "username", Username);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "userId", UserId.ToString());

        OnAuthStateChanged?.Invoke();
    }
}
