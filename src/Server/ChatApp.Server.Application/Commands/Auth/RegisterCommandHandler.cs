using ChatApp.Contracts.Responses;
using ChatApp.Server.Application.Services;
using ChatApp.Server.Domain.Repositories;
using ChatApp.Server.Domain.Services;
using MediatR;

namespace ChatApp.Server.Application.Commands.Auth;

/// <summary>
/// Handler для команды регистрации пользователя
/// </summary>
public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, RegisterResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public RegisterCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<RegisterResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        // Проверка уникальности username
        var existingUser = await _userRepository.GetByUsernameAsync(request.Username, cancellationToken);
        if (existingUser != null)
        {
            return new RegisterResponse
            {
                Success = false,
                ErrorMessage = "Пользователь с таким именем уже существует"
            };
        }

        // Хеширование пароля
        var passwordHash = _passwordHasher.HashPassword(request.Password);

        // Создание нового пользователя
        var user = new ChatApp.Server.Domain.Entities.User(request.Username, passwordHash);

        // Сохранение в репозиторий
        await _userRepository.AddAsync(user, cancellationToken);

        // Генерация JWT токена
        var token = _jwtTokenGenerator.GenerateToken(user.Id, user.Username);

        return new RegisterResponse
        {
            Success = true,
            Token = token,
            UserId = user.Id,
            Username = user.Username
        };
    }
}
