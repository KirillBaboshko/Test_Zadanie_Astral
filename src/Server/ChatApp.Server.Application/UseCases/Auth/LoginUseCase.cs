using ChatApp.Contracts.Requests;
using ChatApp.Contracts.Responses;
using ChatApp.Server.Domain.Repositories;
using ChatApp.Server.Domain.Services;

namespace ChatApp.Server.Application.UseCases.Auth;

public sealed class LoginUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public LoginUseCase(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _jwtTokenGenerator = jwtTokenGenerator ?? throw new ArgumentNullException(nameof(jwtTokenGenerator));
    }

    /// <summary>
    /// Авторизует пользователя
    /// </summary>
    public async Task<AuthResponse?> ExecuteAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        
        var user = await _userRepository.GetByUsernameAsync(request.Username, cancellationToken);
        if (user == null)
            return null; 

        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            return null; 

        // Обновляем время последней активности (ChangeTracker автоматически отследит изменения)
        user.UpdateLastSeen();

        // Генерируем JWT токен
        var token = _jwtTokenGenerator.GenerateToken(user.Id, user.Username);

        return new AuthResponse
        {
            Token = token,
            UserId = user.Id,
            Username = user.Username,
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };
    }
}
