using ChatApp.Server.Domain.Entities;

namespace ChatApp.Server.Domain.Tests.Entities;

/// <summary>
/// Тесты для сущности User
/// </summary>
[TestFixture]
public class UserTests
{
    #region Create Tests

    /// <summary>
    /// Тест: создание пользователя с валидными данными должно быть успешным
    /// </summary>
    [Test]
    public void Create_WithValidData_ShouldCreateUserSuccessfully()
    {
        // Arrange
        var username = "testuser";
        var passwordHash = "hashed_password_123";

        // Act
        var user = User.Create(username, passwordHash);

        // Assert
        Assert.That(user, Is.Not.Null);
        Assert.That(user.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(user.Username, Is.EqualTo(username));
        Assert.That(user.PasswordHash, Is.EqualTo(passwordHash));
        Assert.That(user.CreatedAt, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(1)));
        Assert.That(user.LastLogin, Is.Null);
        Assert.That(user.Messages, Is.Empty);
    }

    /// <summary>
    /// Тест: создание пользователя с пустым username должно выбросить исключение
    /// </summary>
    [TestCase("")]
    [TestCase(null)]
    [TestCase("   ")]
    public void Create_WithEmptyUsername_ShouldThrowArgumentException(string username)
    {
        // Arrange
        var passwordHash = "hashed_password_123";

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => User.Create(username, passwordHash));
        Assert.That(ex.ParamName, Is.EqualTo("username"));
    }

    /// <summary>
    /// Тест: создание пользователя с пустым паролем должно выбросить исключение
    /// </summary>
    [TestCase("")]
    [TestCase(null)]
    [TestCase("   ")]
    public void Create_WithEmptyPasswordHash_ShouldThrowArgumentException(string passwordHash)
    {
        // Arrange
        var username = "testuser";

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => User.Create(username, passwordHash));
        Assert.That(ex.ParamName, Is.EqualTo("passwordHash"));
    }

    #endregion

    #region AddMessage Tests

    /// <summary>
    /// Тест: добавление сообщения с валидным контентом должно быть успешным
    /// </summary>
    [Test]
    public void AddMessage_WithValidContent_ShouldAddMessageSuccessfully()
    {
        // Arrange
        var user = User.Create("testuser", "hashed_password");
        var content = "Hello, World!";

        // Act
        user.AddMessage(content);

        // Assert
        Assert.That(user.Messages, Has.Count.EqualTo(1));
        
        var message = user.Messages.First();
        Assert.That(message.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(message.Content, Is.EqualTo(content));
        Assert.That(message.Timestamp, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(1)));
    }

    /// <summary>
    /// Тест: добавление нескольких сообщений
    /// </summary>
    [Test]
    public void AddMessage_MultipleMessages_ShouldAddAllMessages()
    {
        // Arrange
        var user = User.Create("testuser", "hashed_password");
        var messages = new[] { "Message 1", "Message 2", "Message 3" };

        // Act
        foreach (var msg in messages)
        {
            user.AddMessage(msg);
        }

        // Assert
        Assert.That(user.Messages, Has.Count.EqualTo(3));
        Assert.That(user.Messages.Select(m => m.Content), Is.EquivalentTo(messages));
    }

    /// <summary>
    /// Тест: добавление сообщения с пустым контентом должно выбросить исключение
    /// </summary>
    [TestCase("")]
    [TestCase(null)]
    [TestCase("   ")]
    public void AddMessage_WithEmptyContent_ShouldThrowArgumentException(string content)
    {
        // Arrange
        var user = User.Create("testuser", "hashed_password");

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => user.AddMessage(content));
        Assert.That(ex.ParamName, Is.EqualTo("content"));
    }

    /// <summary>
    /// Тест: добавление сообщения с очень длинным контентом
    /// </summary>
    [Test]
    public void AddMessage_WithVeryLongContent_ShouldAddSuccessfully()
    {
        // Arrange
        var user = User.Create("testuser", "hashed_password");
        var content = new string('A', 5000);

        // Act
        user.AddMessage(content);

        // Assert
        Assert.That(user.Messages, Has.Count.EqualTo(1));
        Assert.That(user.Messages.First().Content, Is.EqualTo(content));
    }

    #endregion

    #region UpdateLastLogin Tests

    /// <summary>
    /// Тест: обновление времени последнего входа
    /// </summary>
    [Test]
    public void UpdateLastLogin_ShouldSetLastLoginToCurrentTime()
    {
        // Arrange
        var user = User.Create("testuser", "hashed_password");
        Assert.That(user.LastLogin, Is.Null);

        // Act
        user.UpdateLastLogin();

        // Assert
        Assert.That(user.LastLogin, Is.Not.Null);
        Assert.That(user.LastLogin!.Value, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(1)));
    }

    /// <summary>
    /// Тест: повторное обновление времени последнего входа
    /// </summary>
    [Test]
    public async Task UpdateLastLogin_CalledMultipleTimes_ShouldUpdateToLatestTime()
    {
        // Arrange
        var user = User.Create("testuser", "hashed_password");
        
        // Act
        user.UpdateLastLogin();
        var firstLoginTime = user.LastLogin;
        
        await Task.Delay(100); // Небольшая задержка
        
        user.UpdateLastLogin();
        var secondLoginTime = user.LastLogin;

        // Assert
        Assert.That(secondLoginTime, Is.GreaterThan(firstLoginTime));
    }

    #endregion

    #region Integration Tests

    /// <summary>
    /// Тест: полный жизненный цикл пользователя
    /// </summary>
    [Test]
    public void UserLifecycle_CreateLoginAddMessages_ShouldWorkCorrectly()
    {
        // Arrange & Act
        var user = User.Create("testuser", "hashed_password");
        
        user.UpdateLastLogin();
        
        user.AddMessage("First message");
        user.AddMessage("Second message");
        user.AddMessage("Third message");

        // Assert
        Assert.That(user.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(user.Username, Is.EqualTo("testuser"));
        Assert.That(user.LastLogin, Is.Not.Null);
        Assert.That(user.Messages, Has.Count.EqualTo(3));
        
        // Проверяем порядок сообщений
        var messageContents = user.Messages.Select(m => m.Content).ToList();
        Assert.That(messageContents[0], Is.EqualTo("First message"));
        Assert.That(messageContents[1], Is.EqualTo("Second message"));
        Assert.That(messageContents[2], Is.EqualTo("Third message"));
    }

    #endregion
}
