using System.Security.Cryptography;
using System.Text;
using ChatApp.Server.Domain.Services;

namespace ChatApp.Server.Infrastructure.Security;


public sealed class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16; // 128 bits
    private const int HashSize = 32; // 256 bits
    private const int Iterations = 100000; // OWASP рекомендует минимум 100,000

    /// <summary>
    /// Хеширует пароль с использованием PBKDF2
    /// </summary>
    public String HashPassword(String password)
    {
        if (String.IsNullOrEmpty(password))
            throw new ArgumentException("Пароль не может быть пустым", nameof(password));

        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);

        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashSize);

        // Формат: iterations.salt.hash (все в Base64)
        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    /// <summary>
    /// Проверяет соответствие пароля хешу
    /// </summary>
    public Boolean VerifyPassword(String password, String passwordHash)
    {
        if (String.IsNullOrEmpty(password))
            return false;

        if (String.IsNullOrEmpty(passwordHash))
            return false;

        try
        {
            // Парсим хеш
            var parts = passwordHash.Split('.');
            if (parts.Length != 3)
                return false;

            var iterations = Int32.Parse(parts[0]);
            var salt = Convert.FromBase64String(parts[1]);
            var hash = Convert.FromBase64String(parts[2]);

            // Хешируем введённый пароль с той же солью
            byte[] testHash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                iterations,
                HashAlgorithmName.SHA256,
                hash.Length);

            // Сравниваем хеши с защитой от timing attacks
            return CryptographicOperations.FixedTimeEquals(hash, testHash);
        }
        catch
        {
            return false;
        }
    }
}
