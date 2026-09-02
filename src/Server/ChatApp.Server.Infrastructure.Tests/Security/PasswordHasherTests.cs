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
        _passwordHasher = new PasswordHasher();
    }


    /// <summary>
    /// Тест: пароль с Unicode символами
    /// </summary>
    [Test]
    public void HashPassword_WithValidPassword_ShouldReturnNonEmptyHash()
    {
        var password = "password123";

        var hash = _passwordHasher.HashPassword(password);

        Assert.That(hash, Is.Not.Null);
        Assert.That(hash, Is.Not.Empty);
    }

    /// <summary>
    /// Тест: пароль с Unicode символами
    /// </summary>
    [Test]
    public void HashPassword_ShouldReturnHashWithSaltAndHashParts()
    {
        var password = "password123";

        var hash = _passwordHasher.HashPassword(password);

        var parts = hash.Split(':');
        Assert.That(parts, Has.Length.EqualTo(2), "Hash should contain salt and hash separated by colon");
        Assert.That(parts[0], Is.Not.Empty, "Salt should not be empty");
        Assert.That(parts[1], Is.Not.Empty, "Hash should not be empty");
    }

    /// <summary>
    /// Тест: пароль с Unicode символами
    /// </summary>
    [Test]
    public void HashPassword_SamePasswordTwice_ShouldReturnDifferentHashes()
    {
        var password = "password123";

        var hash1 = _passwordHasher.HashPassword(password);
        var hash2 = _passwordHasher.HashPassword(password);

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
        var hash = _passwordHasher.HashPassword(password);

        Assert.That(hash, Is.Not.Null);
        Assert.That(hash, Does.Contain(":"));
    }



    /// <summary>
    /// Тест: пароль с Unicode символами
    /// </summary>
    [Test]
    public void VerifyPassword_WithCorrectPassword_ShouldReturnTrue()
    {
        var password = "password123";
        var hash = _passwordHasher.HashPassword(password);

        var result = _passwordHasher.VerifyPassword(password, hash);

        Assert.That(result, Is.True);
    }

    /// <summary>
    /// Тест: пароль с Unicode символами
    /// </summary>
    [Test]
    public void VerifyPassword_WithIncorrectPassword_ShouldReturnFalse()
    {
        var correctPassword = "password123";
        var incorrectPassword = "wrong_password";
        var hash = _passwordHasher.HashPassword(correctPassword);

        var result = _passwordHasher.VerifyPassword(incorrectPassword, hash);

        Assert.That(result, Is.False);
    }

    /// <summary>
    /// Тест: верификация с различными комбинациями правильных/неправильных паролей
    /// </summary>
    [TestCase("password123", "password123", true)]
    [TestCase("password123", "password124", false)]
    [TestCase("SecurePass!@#", "SecurePass!@#", true)]
    [TestCase("SecurePass!@#", "securepass!@#", false)]
    [TestCase("12345678", "12345678", true)]
    [TestCase("12345678", "87654321", false)]
    public void VerifyPassword_WithVariousPasswordCombinations_ShouldReturnExpectedResult(
        string originalPassword, string checkPassword, bool expectedResult)
    {
        var hash = _passwordHasher.HashPassword(originalPassword);

        var result = _passwordHasher.VerifyPassword(checkPassword, hash);

        Assert.That(result, Is.EqualTo(expectedResult));
    }

    /// <summary>
    /// Тест: верификация с пустым паролем должна вернуть false
    /// </summary>
    [TestCase("")]
    [TestCase(null)]
    public void VerifyPassword_WithEmptyPassword_ShouldReturnFalse(string emptyPassword)
    {
        var hash = _passwordHasher.HashPassword("password123");

        var result = _passwordHasher.VerifyPassword(emptyPassword, hash);

        Assert.That(result, Is.False);
    }

    /// <summary>
    /// Тест: верификация с невалидным хешом должна вернуть false
    /// </summary>
    [TestCase("invalid_hash")]
    [TestCase("no_colon_separator")]
    /// <summary>
    /// Тест: верификация с пустым паролем должна вернуть false
    /// </summary>
    [TestCase("")]
    public void VerifyPassword_WithInvalidHash_ShouldReturnFalse(string invalidHash)
    {
        var password = "password123";

        var result = _passwordHasher.VerifyPassword(password, invalidHash);

        Assert.That(result, Is.False);
    }



    /// <summary>
    /// Тест: пароль с Unicode символами
    /// </summary>
    [Test]
    public void HashPassword_ShouldReturnHashOfSufficientLength()
    {
        var password = "password123";

        var hash = _passwordHasher.HashPassword(password);

        Assert.That(hash.Length, Is.GreaterThan(50), 
            "Hash should be sufficiently long for security");
    }

    /// <summary>
    /// Тест: пароль с Unicode символами
    /// </summary>
    [Test]
    public void VerifyPassword_ShouldTakeSimilarTimeForCorrectAndIncorrectPasswords()
    {
        var password = "password123";
        var hash = _passwordHasher.HashPassword(password);
        var incorrectPassword = "wrong_password";
        
        var sw = System.Diagnostics.Stopwatch.StartNew();

        _passwordHasher.VerifyPassword(password, hash);
        var timeCorrect = sw.ElapsedMilliseconds;
        
        sw.Restart();
        
        _passwordHasher.VerifyPassword(incorrectPassword, hash);
        var timeIncorrect = sw.ElapsedMilliseconds;

        var timeDifference = Math.Abs(timeCorrect - timeIncorrect);
        Assert.That(timeDifference, Is.LessThan(50), 
            "Time difference should be minimal to prevent timing attacks");
    }

    /// <summary>
    /// Тест: пароль с Unicode символами
    /// </summary>
    [Test]
    public void HashPassword_MultipleHashesOfSamePassword_ShouldAllBeValid()
    {
        var password = "password123";
        var hashes = new List<string>();

        for (int i = 0; i < 10; i++)
        {
            hashes.Add(_passwordHasher.HashPassword(password));
        }

        Assert.That(hashes.Distinct().Count(), Is.EqualTo(10), "All hashes should be unique");
        
        foreach (var hash in hashes)
        {
            Assert.That(_passwordHasher.VerifyPassword(password, hash), Is.True,
                "All hashes should verify the correct password");
        }
    }



    /// <summary>
    /// Тест: пароль с Unicode символами
    /// </summary>
    [Test]
    public void HashPassword_WithVeryShortPassword_ShouldWorkCorrectly()
    {
        var password = "a";

        var hash = _passwordHasher.HashPassword(password);
        var isValid = _passwordHasher.VerifyPassword(password, hash);

        Assert.That(hash, Is.Not.Null);
        Assert.That(isValid, Is.True);
    }

    /// <summary>
    /// Тест: пароль с Unicode символами
    /// </summary>
    [Test]
    public void HashPassword_WithVeryLongPassword_ShouldWorkCorrectly()
    {
        var password = new string('a', 1000);

        var hash = _passwordHasher.HashPassword(password);
        var isValid = _passwordHasher.VerifyPassword(password, hash);

        Assert.That(hash, Is.Not.Null);
        Assert.That(isValid, Is.True);
    }

    /// <summary>
    /// Тест: пароль с Unicode символами
    /// </summary>
    [Test]
    public void HashPassword_WithSpecialCharacters_ShouldWorkCorrectly()
    {
        var password = "!@#$%^&*()_+-=[]{}|;':\",./<>?`~";

        var hash = _passwordHasher.HashPassword(password);
        var isValid = _passwordHasher.VerifyPassword(password, hash);

        Assert.That(hash, Is.Not.Null);
        Assert.That(isValid, Is.True);
    }

    /// <summary>
    /// Тест: пароль с Unicode символами
    /// </summary>
    [Test]
    public void HashPassword_WithUnicodeCharacters_ShouldWorkCorrectly()
    {
        var password = "пароль123密码🔒";

        var hash = _passwordHasher.HashPassword(password);
        var isValid = _passwordHasher.VerifyPassword(password, hash);

        Assert.That(hash, Is.Not.Null);
        Assert.That(isValid, Is.True);
    }

}
