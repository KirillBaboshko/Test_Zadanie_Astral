namespace ChatApp.Server.Domain.Services;

public interface IJwtTokenGenerator
{
    String GenerateToken(Guid userId, String username);
}
