using ChatApp.Client.Blazor.Services;
using Microsoft.AspNetCore.Components;

namespace ChatApp.Client.Blazor.ViewModels;

/// <summary>
/// ViewModel для страницы авторизации и регистрации
/// </summary>
public class AuthViewModel
{
    private readonly AuthService _authService;
    private readonly NavigationManager _navigationManager;

    public AuthViewModel(AuthService authService, NavigationManager navigationManager)
    {
        _authService = authService;
        _navigationManager = navigationManager;
    }

    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsError { get; set; }
    public bool IsLoading { get; set; }
    public bool IsLoginMode { get; set; } = true;

    /// <summary>
    /// Инициализация - проверка существующей авторизации
    /// </summary>
    public async Task InitializeAsync()
    {
        await _authService.InitializeAsync();
        
        if (_authService.IsAuthenticated)
        {
            _navigationManager.NavigateTo("/chat");
        }
    }

    /// <summary>
    /// Вход в систему
    /// </summary>
    public async Task<bool> LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            ShowMessage("Заполните все поля", true);
            return false;
        }

        IsLoading = true;
        var (success, msg) = await _authService.LoginAsync(Username, Password);
        IsLoading = false;

        if (success)
        {
            ShowMessage(msg, false);
            await Task.Delay(500);
            _navigationManager.NavigateTo("/chat");
            return true;
        }

        ShowMessage(msg, true);
        return false;
    }

    /// <summary>
    /// Регистрация нового пользователя
    /// </summary>
    public async Task<bool> RegisterAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            ShowMessage("Заполните все поля", true);
            return false;
        }

        if (Password.Length < 4)
        {
            ShowMessage("Пароль должен быть не менее 4 символов", true);
            return false;
        }

        IsLoading = true;
        var (success, msg) = await _authService.RegisterAsync(Username, Password);
        IsLoading = false;

        if (success)
        {
            ShowMessage(msg, false);
            await Task.Delay(500);
            _navigationManager.NavigateTo("/chat");
            return true;
        }

        ShowMessage(msg, true);
        return false;
    }

    /// <summary>
    /// Переключение на режим регистрации
    /// </summary>
    public void SwitchToRegister()
    {
        IsLoginMode = false;
        Message = string.Empty;
    }

    /// <summary>
    /// Переключение на режим входа
    /// </summary>
    public void SwitchToLogin()
    {
        IsLoginMode = true;
        Message = string.Empty;
    }

    /// <summary>
    /// Отображение сообщения
    /// </summary>
    private void ShowMessage(string msg, bool error)
    {
        Message = msg;
        IsError = error;
    }
}
