using ChatApp.Contracts.Responses;
using ChatApp.Server.Application.Services;
using ChatApp.Server.Domain.Repositories;
using ChatApp.Server.Domain.Services;
using MediatR;

namespace ChatApp.Server.Application.Commands.Auth;

/// <summary>
/// Handler для команды входа в систему
/// </summary>
public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByUsernameAsync(request.Username, cancellationToken);
        if (user == null)
        {
            return new LoginResponse
            {
                Success = false,
                ErrorMessage = "Неверное имя пользователя или пароль"
            };
        }

        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            return new LoginResponse
            {
                Success = false,
                ErrorMessage = "Неверное имя пользователя или пароль"
            };
        }

        user.UpdateLastSeen();

        var token = _jwtTokenGenerator.GenerateToken(user.Id, user.Username);

        return new LoginResponse
        {
            Success = true,
            Token = token,
            UserId = user.Id,
            Username = user.Username
        };
    }
}
