namespace ChatApp.Contracts.Requests;

public sealed class LoginRequest
{
    public String Username { get; set; } = String.Empty;
    public String Password { get; set; } = String.Empty;
}
