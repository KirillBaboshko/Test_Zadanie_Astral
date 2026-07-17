using ChatApp.Contracts.Requests;
using ChatApp.Contracts.Responses;
using ChatApp.Server.Application.Common;
using ChatApp.Server.Domain.Abstractions;
using ChatApp.Server.Domain.Entities;
using ChatApp.Server.Domain.Repositories;
using ChatApp.Server.Domain.Services;

namespace ChatApp.Server.Application.UseCases.Auth;

/// <summary>
/// Use case для регистрации нового пользователя
/// </summary>
public sealed class RegisterUseCase : UseCaseBase
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public RegisterUseCase(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IUnitOfWork unitOfWork) : base(unitOfWork)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _jwtTokenGenerator = jwtTokenGenerator ?? throw new ArgumentNullException(nameof(jwtTokenGenerator));
    }

    /// <summary>
    /// Регистрирует нового пользователя
    /// </summary>
    public async Task<AuthResponse?> ExecuteAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithUnitOfWorkAsync(async ct =>
        {
            var existingUser = await _userRepository.GetByUsernameAsync(request.Username, ct);
            if (existingUser != null)
                return null; 

            var passwordHash = _passwordHasher.HashPassword(request.Password);

            var user = new User(request.Username, passwordHash);

            await _userRepository.AddAsync(user, ct);

            var token = _jwtTokenGenerator.GenerateToken(user.Id, user.Username);

            return new AuthResponse
            {
                Token = token,
                UserId = user.Id,
                Username = user.Username,
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };
        }, cancellationToken);
    }
}
