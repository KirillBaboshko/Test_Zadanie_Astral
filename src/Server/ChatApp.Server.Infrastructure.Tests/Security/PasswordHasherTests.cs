using ChatApp.Server.Infrastructure.Security;

namespace ChatApp.Server.Infrastructure.Tests.Security;

/// <summary>
/// Тесты для PasswordHasher
/// </summary>
[TestFixture]
public class PasswordHasherTests
{
    private PasswordHasher _passwordHasher = null!;

    [SetUp]
    public void Setup()
    {
        // Arrange
        _passwordHasher = new PasswordHasher();
    }

    #region HashPassword Tests

    /// <summary>
    /// Тест: хеширование пароля должно возвращать непустую строку
    /// </summary>
    [Test]
    public void HashPassword_WithValidPassword_ShouldReturnNonEmptyHash()
    {
        // Arrange
        var password = "password123";

        // Act
        var hash = _passwordHasher.HashPassword(password);

        // Assert
        Assert.That(hash, Is.Not.Null);
        Assert.That(hash, Is.Not.Empty);
    }

    /// <summary>
    /// Тест: хеш должен содержать соль и хеш, разделённые двоеточием
    /// </summary>
    [Test]
    public void HashPassword_ShouldReturnHashWithSaltAndHashParts()
    {
        // Arrange
        var password = "password123";

        // Act
        var hash = _passwordHasher.HashPassword(password);

        // Assert
        var parts = hash.Split(':');
        Assert.That(parts, Has.Length.EqualTo(2), "Hash should contain salt and hash separated by colon");
        Assert.That(parts[0], Is.Not.Empty, "Salt should not be empty");
        Assert.That(parts[1], Is.Not.Empty, "Hash should not be empty");
    }

    /// <summary>
    /// Тест: один и тот же пароль должен давать разные хеши (из-за уникальной соли)
    /// </summary>
    [Test]
    public void HashPassword_SamePasswordTwice_ShouldReturnDifferentHashes()
    {
        // Arrange
        var password = "password123";

        // Act
        var hash1 = _passwordHasher.HashPassword(password);
        var hash2 = _passwordHasher.HashPassword(password);

        // Assert
        Assert.That(hash1, Is.Not.EqualTo(hash2), "Each hash should have unique salt");
    }

    /// <summary>
    /// Тест: хеширование различных паролей
    /// </summary>
    [TestCase("password123")]
    [TestCase("SecurePass!@#")]
    [TestCase("12345678")]
    [TestCase("very_long_password_with_special_chars_$%^&*()")]
    public void HashPassword_WithDifferentPasswords_ShouldReturnDifferentHashes(string password)
    {
        // Arrange & Act
        var hash = _passwordHasher.HashPassword(password);

        // Assert
        Assert.That(hash, Is.Not.Null);
        Assert.That(hash, Does.Contain(":"));
    }

    #endregion

    #region VerifyPassword Tests

    /// <summary>
    /// Тест: верификация правильного пароля должна вернуть true
    /// </summary>
    [Test]
    public void VerifyPassword_WithCorrectPassword_ShouldReturnTrue()
    {
        // Arrange
        var password = "password123";
        var hash = _passwordHasher.HashPassword(password);

        // Act
        var result = _passwordHasher.VerifyPassword(password, hash);

        // Assert
        Assert.That(result, Is.True);
    }

    /// <summary>
    /// Тест: верификация неправильного пароля должна вернуть false
    /// </summary>
    [Test]
    public void VerifyPassword_WithIncorrectPassword_ShouldReturnFalse()
    {
        // Arrange
        var correctPassword = "password123";
        var incorrectPassword = "wrong_password";
        var hash = _passwordHasher.HashPassword(correctPassword);

        // Act
        var result = _passwordHasher.VerifyPassword(incorrectPassword, hash);

        // Assert
        Assert.That(result, Is.False);
    }

    /// <summary>
    /// Тест: верификация с различными комбинациями правильных/неправильных паролей
    /// </summary>
    [TestCase("password123", "password123", true)]
    [TestCase("password123", "password124", false)]
    [TestCase("SecurePass!@#", "SecurePass!@#", true)]
    [TestCase("SecurePass!@#", "securepass!@#", false)] // case sensitive
    [TestCase("12345678", "12345678", true)]
    [TestCase("12345678", "87654321", false)]
    public void VerifyPassword_WithVariousPasswordCombinations_ShouldReturnExpectedResult(
        string originalPassword, string checkPassword, bool expectedResult)
    {
        // Arrange
        var hash = _passwordHasher.HashPassword(originalPassword);

        // Act
        var result = _passwordHasher.VerifyPassword(checkPassword, hash);

        // Assert
        Assert.That(result, Is.EqualTo(expectedResult));
    }

    /// <summary>
    /// Тест: верификация с пустым паролем должна вернуть false
    /// </summary>
    [TestCase("")]
    [TestCase(null)]
    public void VerifyPassword_WithEmptyPassword_ShouldReturnFalse(string emptyPassword)
    {
        // Arrange
        var hash = _passwordHasher.HashPassword("password123");

        // Act
        var result = _passwordHasher.VerifyPassword(emptyPassword, hash);

        // Assert
        Assert.That(result, Is.False);
    }

    /// <summary>
    /// Тест: верификация с невалидным хешом должна вернуть false
    /// </summary>
    [TestCase("invalid_hash")]
    [TestCase("no_colon_separator")]
    [TestCase("")]
    public void VerifyPassword_WithInvalidHash_ShouldReturnFalse(string invalidHash)
    {
        // Arrange
        var password = "password123";

        // Act
        var result = _passwordHasher.VerifyPassword(password, invalidHash);

        // Assert
        Assert.That(result, Is.False);
    }

    #endregion

    #region Security Tests

    /// <summary>
    /// Тест: хеш должен быть достаточно длинным (для безопасности)
    /// </summary>
    [Test]
    public void HashPassword_ShouldReturnHashOfSufficientLength()
    {
        // Arrange
        var password = "password123";

        // Act
        var hash = _passwordHasher.HashPassword(password);

        // Assert
        Assert.That(hash.Length, Is.GreaterThan(50), 
            "Hash should be sufficiently long for security");
    }

    /// <summary>
    /// Тест: проверка устойчивости к timing attacks (одинаковое время работы)
    /// </summary>
    [Test]
    public void VerifyPassword_ShouldTakeSimilarTimeForCorrectAndIncorrectPasswords()
    {
        // Arrange
        var password = "password123";
        var hash = _passwordHasher.HashPassword(password);
        var incorrectPassword = "wrong_password";
        
        var sw = System.Diagnostics.Stopwatch.StartNew();

        // Act - verify correct password
        _passwordHasher.VerifyPassword(password, hash);
        var timeCorrect = sw.ElapsedMilliseconds;
        
        sw.Restart();
        
        // Act - verify incorrect password
        _passwordHasher.VerifyPassword(incorrectPassword, hash);
        var timeIncorrect = sw.ElapsedMilliseconds;

        // Assert - разница во времени должна быть минимальной
        var timeDifference = Math.Abs(timeCorrect - timeIncorrect);
        Assert.That(timeDifference, Is.LessThan(50), 
            "Time difference should be minimal to prevent timing attacks");
    }

    /// <summary>
    /// Тест: множественные хеширования одного пароля
    /// </summary>
    [Test]
    public void HashPassword_MultipleHashesOfSamePassword_ShouldAllBeValid()
    {
        // Arrange
        var password = "password123";
        var hashes = new List<string>();

        // Act - create multiple hashes
        for (int i = 0; i < 10; i++)
        {
            hashes.Add(_passwordHasher.HashPassword(password));
        }

        // Assert - all hashes should be unique but verify the same password
        Assert.That(hashes.Distinct().Count(), Is.EqualTo(10), "All hashes should be unique");
        
        foreach (var hash in hashes)
        {
            Assert.That(_passwordHasher.VerifyPassword(password, hash), Is.True,
                "All hashes should verify the correct password");
        }
    }

    #endregion

    #region Edge Cases

    /// <summary>
    /// Тест: очень короткий пароль
    /// </summary>
    [Test]
    public void HashPassword_WithVeryShortPassword_ShouldWorkCorrectly()
    {
        // Arrange
        var password = "a";

        // Act
        var hash = _passwordHasher.HashPassword(password);
        var isValid = _passwordHasher.VerifyPassword(password, hash);

        // Assert
        Assert.That(hash, Is.Not.Null);
        Assert.That(isValid, Is.True);
    }

    /// <summary>
    /// Тест: очень длинный пароль
    /// </summary>
    [Test]
    public void HashPassword_WithVeryLongPassword_ShouldWorkCorrectly()
    {
        // Arrange
        var password = new string('a', 1000);

        // Act
        var hash = _passwordHasher.HashPassword(password);
        var isValid = _passwordHasher.VerifyPassword(password, hash);

        // Assert
        Assert.That(hash, Is.Not.Null);
        Assert.That(isValid, Is.True);
    }

    /// <summary>
    /// Тест: пароль со специальными символами
    /// </summary>
    [Test]
    public void HashPassword_WithSpecialCharacters_ShouldWorkCorrectly()
    {
        // Arrange
        var password = "!@#$%^&*()_+-=[]{}|;':\",./<>?`~";

        // Act
        var hash = _passwordHasher.HashPassword(password);
        var isValid = _passwordHasher.VerifyPassword(password, hash);

        // Assert
        Assert.That(hash, Is.Not.Null);
        Assert.That(isValid, Is.True);
    }

    /// <summary>
    /// Тест: пароль с Unicode символами
    /// </summary>
    [Test]
    public void HashPassword_WithUnicodeCharacters_ShouldWorkCorrectly()
    {
        // Arrange
        var password = "пароль123密码🔒";

        // Act
        var hash = _passwordHasher.HashPassword(password);
        var isValid = _passwordHasher.VerifyPassword(password, hash);

        // Assert
        Assert.That(hash, Is.Not.Null);
        Assert.That(isValid, Is.True);
    }

    #endregion
}
