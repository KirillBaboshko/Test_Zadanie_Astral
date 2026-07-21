namespace ChatApp.Contracts.Responses;

public sealed class AuthResponse
{
    public String Token { get; set; } = String.Empty;
    public Guid UserId { get; set; }
    public String Username { get; set; } = String.Empty;
    public DateTime ExpiresAt { get; set; }
}
