using ProtoBuf.Grpc;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace ChatApp.Shared.Grpc.Contracts;

/// <summary>
/// Контракт сервиса аутентификации (code-first)
/// </summary>
[ServiceContract]
public interface IAuthService
{
    [OperationContract]
    Task<AuthResponse> Register(RegisterRequest request, CallContext context = default);

    [OperationContract]
    Task<AuthResponse> Login(LoginRequest request, CallContext context = default);
}

[DataContract]
public class RegisterRequest
{
    [DataMember(Order = 1)]
    public string Username { get; set; } = string.Empty;

    [DataMember(Order = 2)]
    public string Password { get; set; } = string.Empty;
}

[DataContract]
public class LoginRequest
{
    [DataMember(Order = 1)]
    public string Username { get; set; } = string.Empty;

    [DataMember(Order = 2)]
    public string Password { get; set; } = string.Empty;
}

[DataContract]
public class AuthResponse
{
    [DataMember(Order = 1)]
    public string Token { get; set; } = string.Empty;

    [DataMember(Order = 2)]
    public string Username { get; set; } = string.Empty;

    [DataMember(Order = 3)]
    public long ExpiresAt { get; set; }

    [DataMember(Order = 4)]
    public string Error { get; set; } = string.Empty;
}
