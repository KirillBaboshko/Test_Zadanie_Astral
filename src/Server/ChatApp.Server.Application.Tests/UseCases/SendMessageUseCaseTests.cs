using ChatApp.Contracts.Requests;
using ChatApp.Server.Application.UseCases.SendMessage;
using ChatApp.Server.Domain.Abstractions;
using ChatApp.Server.Domain.Entities;
using ChatApp.Server.Domain.Repositories;
using NSubstitute;

namespace ChatApp.Server.Application.Tests.UseCases;

/// <summary>
/// Тесты для SendMessageUseCase
/// </summary>
[TestFixture]
public class SendMessageUseCaseTests
{
    private IUserRepository _userRepository = null!;
    private IUnitOfWork _unitOfWork = null!;
    private SendMessageUseCase _useCase = null!;

    [SetUp]
    public void Setup()
    {
        // Arrange
        _userRepository = Substitute.For<IUserRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _unitOfWork.Users.Returns(_userRepository);
        
        _useCase = new SendMessageUseCase(_unitOfWork);
    }

    /// <summary>
    /// Тест: успешная отправка сообщения
    /// </summary>
    [Test]
    public async Task ExecuteAuthAsync_WithValidData_ShouldSendMessageSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new SendMessageAuthRequest
        {
            Content = "Hello, World!"
        };
        
        var user = User.Create("testuser", "hashed_password");
        
        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<User?>(user));
        
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        // Act
        var result = await _useCase.ExecuteAuthAsync(userId, request, CancellationToken.None);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.SenderName, Is.EqualTo("testuser"));
        Assert.That(result.Content, Is.EqualTo("Hello, World!"));
        Assert.That(result.Timestamp, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(1)));
        
        // Проверяем, что методы были вызваны
        await _userRepository.Received(1).GetByIdAsync(userId, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Тест: отправка сообщения несуществующим пользователем
    /// </summary>
    [Test]
    public async Task ExecuteAuthAsync_WithNonExistentUser_ShouldReturnNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new SendMessageAuthRequest
        {
            Content = "Hello, World!"
        };
        
        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<User?>(null));

        // Act
        var result = await _useCase.ExecuteAuthAsync(userId, request, CancellationToken.None);

        // Assert
        Assert.That(result, Is.Null);
        
        // SaveChangesAsync не должен быть вызван
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Тест: отправка нескольких сообщений одним пользователем
    /// </summary>
    [TestCase("First message")]
    [TestCase("Second message")]
    [TestCase("Third message with special chars: !@#$%^&*()")]
    public async Task ExecuteAuthAsync_MultipleMessages_ShouldSendAllSuccessfully(string content)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new SendMessageAuthRequest
        {
            Content = content
        };
        
        var user = User.Create("testuser", "hashed_password");
        
        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<User?>(user));
        
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        // Act
        var result = await _useCase.ExecuteAuthAsync(userId, request, CancellationToken.None);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Content, Is.EqualTo(content));
    }

    /// <summary>
    /// Тест: отправка длинного сообщения
    /// </summary>
    [Test]
    public async Task ExecuteAuthAsync_WithLongMessage_ShouldSendSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var longContent = new string('A', 5000);
        var request = new SendMessageAuthRequest
        {
            Content = longContent
        };
        
        var user = User.Create("testuser", "hashed_password");
        
        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<User?>(user));
        
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        // Act
        var result = await _useCase.ExecuteAuthAsync(userId, request, CancellationToken.None);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Content, Has.Length.EqualTo(5000));
    }

    /// <summary>
    /// Тест: проверка что сообщение добавляется к пользователю
    /// </summary>
    [Test]
    public async Task ExecuteAuthAsync_ShouldAddMessageToUserAggregateRoot()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new SendMessageAuthRequest
        {
            Content = "Test message"
        };
        
        var user = User.Create("testuser", "hashed_password");
        var initialMessageCount = user.Messages.Count;
        
        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<User?>(user));
        
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        // Act
        await _useCase.ExecuteAuthAsync(userId, request, CancellationToken.None);

        // Assert
        Assert.That(user.Messages.Count, Is.EqualTo(initialMessageCount + 1));
        Assert.That(user.Messages.Last().Content, Is.EqualTo("Test message"));
    }
}
