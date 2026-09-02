using ChatApp.Contracts.Requests;
using ChatApp.Contracts.Responses;
using ChatApp.Server.Domain.Repositories;
using ChatApp.Server.Domain.Services;
using ChatApp.Shared.Messages.Events;
using MassTransit;

namespace ChatApp.Server.Application.UseCases.Auth;

public sealed class LoginUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IPublishEndpoint _publishEndpoint;

    public LoginUseCase(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IPublishEndpoint publishEndpoint)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _jwtTokenGenerator = jwtTokenGenerator ?? throw new ArgumentNullException(nameof(jwtTokenGenerator));
        _publishEndpoint = publishEndpoint ?? throw new ArgumentNullException(nameof(publishEndpoint));
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

        user.UpdateLastSeen();

        await _publishEndpoint.Publish(new UserLoggedInEvent
        {
            UserId = user.Id,
            Username = user.Username,
            LoggedInAt = DateTime.UtcNow
        }, cancellationToken);

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
