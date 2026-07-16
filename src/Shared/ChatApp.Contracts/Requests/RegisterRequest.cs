namespace ChatApp.Contracts.Requests;

public sealed class RegisterRequest
{
    public String Username { get; set; } = String.Empty;
    public String Password { get; set; } = String.Empty;
}
