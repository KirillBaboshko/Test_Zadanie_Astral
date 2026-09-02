using ChatApp.Server.Domain.Entities;

namespace ChatApp.Server.Domain.Tests.Entities;

/// <summary>
/// Тесты для сущности User
/// </summary>
[TestFixture]
public class UserTests
{

    /// <summary>
    /// Тест: полный жизненный цикл пользователя
    /// </summary>
    [Test]
    public void Create_WithValidData_ShouldCreateUserSuccessfully()
    {
        var username = "testuser";
        var passwordHash = "hashed_password_123";

        var user = User.Create(username, passwordHash);

        Assert.That(user, Is.Not.Null);
        Assert.That(user.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(user.Username, Is.EqualTo(username));
        Assert.That(user.PasswordHash, Is.EqualTo(passwordHash));
        Assert.That(user.CreatedAt, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(1)));
        Assert.That(user.LastLogin, Is.Null);
        Assert.That(user.Messages, Is.Empty);
    }

    /// <summary>
    /// Тест: добавление сообщения с пустым контентом должно выбросить исключение
    /// </summary>
    [TestCase("")]
    [TestCase(null)]
    [TestCase("   ")]
    public void Create_WithEmptyUsername_ShouldThrowArgumentException(string username)
    {
        var passwordHash = "hashed_password_123";

        var ex = Assert.Throws<ArgumentException>(() => User.Create(username, passwordHash));
        Assert.That(ex.ParamName, Is.EqualTo("username"));
    }

    /// <summary>
    /// Тест: добавление сообщения с пустым контентом должно выбросить исключение
    /// </summary>
    [TestCase("")]
    [TestCase(null)]
    [TestCase("   ")]
    public void Create_WithEmptyPasswordHash_ShouldThrowArgumentException(string passwordHash)
    {
        var username = "testuser";

        var ex = Assert.Throws<ArgumentException>(() => User.Create(username, passwordHash));
        Assert.That(ex.ParamName, Is.EqualTo("passwordHash"));
    }



    /// <summary>
    /// Тест: полный жизненный цикл пользователя
    /// </summary>
    [Test]
    public void AddMessage_WithValidContent_ShouldAddMessageSuccessfully()
    {
        var user = User.Create("testuser", "hashed_password");
        var content = "Hello, World!";

        user.AddMessage(content);

        Assert.That(user.Messages, Has.Count.EqualTo(1));
        
        var message = user.Messages.First();
        Assert.That(message.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(message.Content, Is.EqualTo(content));
        Assert.That(message.Timestamp, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(1)));
    }

    /// <summary>
    /// Тест: полный жизненный цикл пользователя
    /// </summary>
    [Test]
    public void AddMessage_MultipleMessages_ShouldAddAllMessages()
    {
        var user = User.Create("testuser", "hashed_password");
        var messages = new[] { "Message 1", "Message 2", "Message 3" };

        foreach (var msg in messages)
        {
            user.AddMessage(msg);
        }

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
        var user = User.Create("testuser", "hashed_password");

        var ex = Assert.Throws<ArgumentException>(() => user.AddMessage(content));
        Assert.That(ex.ParamName, Is.EqualTo("content"));
    }

    /// <summary>
    /// Тест: полный жизненный цикл пользователя
    /// </summary>
    [Test]
    public void AddMessage_WithVeryLongContent_ShouldAddSuccessfully()
    {
        var user = User.Create("testuser", "hashed_password");
        var content = new string('A', 5000);

        user.AddMessage(content);

        Assert.That(user.Messages, Has.Count.EqualTo(1));
        Assert.That(user.Messages.First().Content, Is.EqualTo(content));
    }



    /// <summary>
    /// Тест: полный жизненный цикл пользователя
    /// </summary>
    [Test]
    public void UpdateLastLogin_ShouldSetLastLoginToCurrentTime()
    {
        var user = User.Create("testuser", "hashed_password");
        Assert.That(user.LastLogin, Is.Null);

        user.UpdateLastLogin();

        Assert.That(user.LastLogin, Is.Not.Null);
        Assert.That(user.LastLogin!.Value, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(1)));
    }

    /// <summary>
    /// Тест: полный жизненный цикл пользователя
    /// </summary>
    [Test]
    public async Task UpdateLastLogin_CalledMultipleTimes_ShouldUpdateToLatestTime()
    {
        var user = User.Create("testuser", "hashed_password");
        
        user.UpdateLastLogin();
        var firstLoginTime = user.LastLogin;
        
        await Task.Delay(100);
        
        user.UpdateLastLogin();
        var secondLoginTime = user.LastLogin;

        Assert.That(secondLoginTime, Is.GreaterThan(firstLoginTime));
    }



    /// <summary>
    /// Тест: полный жизненный цикл пользователя
    /// </summary>
    [Test]
    public void UserLifecycle_CreateLoginAddMessages_ShouldWorkCorrectly()
    {
        var user = User.Create("testuser", "hashed_password");
        
        user.UpdateLastLogin();
        
        user.AddMessage("First message");
        user.AddMessage("Second message");
        user.AddMessage("Third message");

        Assert.That(user.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(user.Username, Is.EqualTo("testuser"));
        Assert.That(user.LastLogin, Is.Not.Null);
        Assert.That(user.Messages, Has.Count.EqualTo(3));
        
        var messageContents = user.Messages.Select(m => m.Content).ToList();
        Assert.That(messageContents[0], Is.EqualTo("First message"));
        Assert.That(messageContents[1], Is.EqualTo("Second message"));
        Assert.That(messageContents[2], Is.EqualTo("Third message"));
    }

}
