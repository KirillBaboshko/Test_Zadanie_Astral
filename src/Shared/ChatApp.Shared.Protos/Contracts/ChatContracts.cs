using ProtoBuf.Grpc;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace ChatApp.Shared.Grpc.Contracts;

/// <summary>
/// Контракт сервиса чата (code-first)
/// </summary>
[ServiceContract]
public interface IChatService
{
    [OperationContract]
    Task<MessageResponse> SendMessage(SendMessageRequest request, CallContext context = default);

    [OperationContract]
    Task<MessagesListResponse> GetMessages(GetMessagesRequest request, CallContext context = default);

    [OperationContract]
    Task<MessagesListResponse> GetMessagesByUser(GetMessagesByUserRequest request, CallContext context = default);

    [OperationContract]
    Task<UsersListResponse> GetUsers(GetUsersRequest request, CallContext context = default);

    [OperationContract]
    IAsyncEnumerable<MessageResponse> StreamMessages(StreamMessagesRequest request, CallContext context = default);
}

[DataContract]
public class SendMessageRequest
{
    [DataMember(Order = 1)]
    public string Content { get; set; } = string.Empty;

    [DataMember(Order = 2)]
    public string Token { get; set; } = string.Empty;
}

[DataContract]
public class GetMessagesRequest
{
    [DataMember(Order = 1)]
    public long SinceTimestamp { get; set; }

    [DataMember(Order = 2)]
    public int Limit { get; set; }
}

[DataContract]
public class GetMessagesByUserRequest
{
    [DataMember(Order = 1)]
    public string Username { get; set; } = string.Empty;

    [DataMember(Order = 2)]
    public int Limit { get; set; }
}

[DataContract]
public class GetUsersRequest
{
}

[DataContract]
public class StreamMessagesRequest
{
    [DataMember(Order = 1)]
    public string Token { get; set; } = string.Empty;

    [DataMember(Order = 2)]
    public long SinceTimestamp { get; set; }
}

[DataContract]
public class MessageResponse
{
    [DataMember(Order = 1)]
    public string Id { get; set; } = string.Empty;

    [DataMember(Order = 2)]
    public string SenderName { get; set; } = string.Empty;

    [DataMember(Order = 3)]
    public string Content { get; set; } = string.Empty;

    [DataMember(Order = 4)]
    public long Timestamp { get; set; }
}

[DataContract]
public class MessagesListResponse
{
    [DataMember(Order = 1)]
    public List<MessageResponse> Messages { get; set; } = new();

    [DataMember(Order = 2)]
    public int TotalCount { get; set; }
}

[DataContract]
public class UserInfo
{
    [DataMember(Order = 1)]
    public string Id { get; set; } = string.Empty;

    [DataMember(Order = 2)]
    public string Username { get; set; } = string.Empty;
}

[DataContract]
public class UsersListResponse
{
    [DataMember(Order = 1)]
    public List<UserInfo> Users { get; set; } = new();
}
