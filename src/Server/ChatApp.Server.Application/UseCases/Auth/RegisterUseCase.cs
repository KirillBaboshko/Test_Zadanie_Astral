using ChatApp.Contracts.Requests;
using ChatApp.Contracts.Responses;
using ChatApp.Server.Domain.Abstractions;
using ChatApp.Server.Domain.Entities;
using ChatApp.Server.Domain.Repositories;
using ChatApp.Server.Domain.Services;

namespace ChatApp.Server.Application.UseCases.Auth;


public sealed class RegisterUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterUseCase(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _jwtTokenGenerator = jwtTokenGenerator ?? throw new ArgumentNullException(nameof(jwtTokenGenerator));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    /// <summary>
    /// Регистрирует нового пользователя
    /// </summary>
    public async Task<AuthResponse?> ExecuteAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        // Проверяем, не существует ли уже пользователь с таким именем
        var existingUser = await _userRepository.GetByUsernameAsync(request.Username, cancellationToken);
        if (existingUser != null)
            return null; // Пользователь уже существует

        // Хешируем пароль
        var passwordHash = _passwordHasher.HashPassword(request.Password);

        // Создаём нового пользователя
        var user = new User(request.Username, passwordHash);

        // Сохраняем в БД
        await _userRepository.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

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
