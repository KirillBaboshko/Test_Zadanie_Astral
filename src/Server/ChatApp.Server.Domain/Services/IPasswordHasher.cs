namespace ChatApp.Server.Domain.Services;

public interface IPasswordHasher
{
    String HashPassword(String password);
    Boolean VerifyPassword(String password, String passwordHash);
}
