using ChatApp.Contracts.Requests;
using ChatApp.Server.Application.UseCases.Auth;
using ChatApp.Server.Domain.Abstractions;
using ChatApp.Server.Domain.Entities;
using ChatApp.Server.Domain.Repositories;
using NSubstitute;

namespace ChatApp.Server.Application.Tests.UseCases;

/// <summary>
/// Тесты для RegisterUseCase с использованием моков (NSubstitute)
/// </summary>
[TestFixture]
public class RegisterUseCaseTests
{
    private IUserRepository _userRepository = null!;
    private IPasswordHasher _passwordHasher = null!;
    private IJwtTokenGenerator _tokenGenerator = null!;
    private IUnitOfWork _unitOfWork = null!;
    private RegisterUseCase _useCase = null!;

    [SetUp]
    public void Setup()
    {
        // Arrange - создаём моки для всех зависимостей
        _userRepository = Substitute.For<IUserRepository>();
        _passwordHasher = Substitute.For<IPasswordHasher>();
        _tokenGenerator = Substitute.For<IJwtTokenGenerator>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        
        // Настраиваем UnitOfWork для возврата мок-репозитория
        _unitOfWork.Users.Returns(_userRepository);
        
        _useCase = new RegisterUseCase(_unitOfWork, _passwordHasher, _tokenGenerator);
    }

    /// <summary>
    /// Тест: успешная регистрация нового пользователя
    /// </summary>
    [Test]
    public async Task ExecuteAsync_WithValidRequest_ShouldRegisterUserAndReturnToken()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "newuser",
            Password = "password123"
        };
        
        var hashedPassword = "hashed_password_123";
        var expectedToken = "jwt_token_xyz";
        
        // Настраиваем моки
        _userRepository.ExistsAsync(request.Username, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));
        
        _passwordHasher.HashPassword(request.Password)
            .Returns(hashedPassword);
        
        _tokenGenerator.GenerateToken(Arg.Any<Guid>(), request.Username)
            .Returns(expectedToken);
        
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        // Act
        var result = await _useCase.ExecuteAsync(request, CancellationToken.None);

        // Assert
        Assert.That(result, Is.EqualTo(expectedToken));
        
        // Проверяем, что методы были вызваны
        await _userRepository.Received(1).ExistsAsync(request.Username, Arg.Any<CancellationToken>());
        _passwordHasher.Received(1).HashPassword(request.Password);
        await _userRepository.Received(1).AddAsync(Arg.Is<User>(u => 
            u.Username == request.Username && 
            u.PasswordHash == hashedPassword), 
            Arg.Any<CancellationToken>());
        _tokenGenerator.Received(1).GenerateToken(Arg.Any<Guid>(), request.Username);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Тест: регистрация с уже существующим username должна выбросить исключение
    /// </summary>
    [Test]
    public void ExecuteAsync_WithExistingUsername_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "existinguser",
            Password = "password123"
        };
        
        _userRepository.ExistsAsync(request.Username, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        // Act & Assert
        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _useCase.ExecuteAsync(request, CancellationToken.None));
        
        Assert.That(ex.Message, Does.Contain("Пользователь с таким именем уже существует"));
        
        // Проверяем, что другие методы НЕ были вызваны
        _passwordHasher.DidNotReceive().HashPassword(Arg.Any<String>());
        await _userRepository.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Тест: регистрация с различными username
    /// </summary>
    [TestCase("user1", "password1")]
    [TestCase("alice123", "SecurePass!123")]
    [TestCase("bob_test", "12345678")]
    public async Task ExecuteAsync_WithDifferentUsernames_ShouldRegisterSuccessfully(
        string username, string password)
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = username,
            Password = password
        };
        
        _userRepository.ExistsAsync(username, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));
        
        _passwordHasher.HashPassword(password)
            .Returns($"hashed_{password}");
        
        _tokenGenerator.GenerateToken(Arg.Any<Guid>(), username)
            .Returns($"token_{username}");
        
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        // Act
        var result = await _useCase.ExecuteAsync(request, CancellationToken.None);

        // Assert
        Assert.That(result, Is.EqualTo($"token_{username}"));
        await _userRepository.Received(1).ExistsAsync(username, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Тест: проверка порядка вызова методов
    /// </summary>
    [Test]
    public async Task ExecuteAsync_ShouldCallMethodsInCorrectOrder()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "testuser",
            Password = "password"
        };
        
        var callOrder = new List<string>();
        
        _userRepository.ExistsAsync(Arg.Any<String>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                callOrder.Add("ExistsAsync");
                return Task.FromResult(false);
            });
        
        _passwordHasher.HashPassword(Arg.Any<String>())
            .Returns(callInfo =>
            {
                callOrder.Add("HashPassword");
                return "hashed";
            });
        
        _userRepository.AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                callOrder.Add("AddAsync");
                return Task.CompletedTask;
            });
        
        _tokenGenerator.GenerateToken(Arg.Any<Guid>(), Arg.Any<String>())
            .Returns(callInfo =>
            {
                callOrder.Add("GenerateToken");
                return "token";
            });
        
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                callOrder.Add("SaveChangesAsync");
                return Task.FromResult(1);
            });

        // Act
        await _useCase.ExecuteAsync(request, CancellationToken.None);

        // Assert
        Assert.That(callOrder, Is.EqualTo(new[]
        {
            "ExistsAsync",
            "HashPassword",
            "AddAsync",
            "GenerateToken",
            "SaveChangesAsync"
        }));
    }

    /// <summary>
    /// Тест: использование CancellationToken
    /// </summary>
    [Test]
    public void ExecuteAsync_WithCancelledToken_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "testuser",
            Password = "password"
        };
        
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Отменяем токен

        _userRepository.ExistsAsync(Arg.Any<String>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var token = callInfo.Arg<CancellationToken>();
                token.ThrowIfCancellationRequested();
                return Task.FromResult(false);
            });

        // Act & Assert
        Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await _useCase.ExecuteAsync(request, cts.Token));
    }
}
