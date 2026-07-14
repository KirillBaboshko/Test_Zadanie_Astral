using ChatApp.Contracts.Messages;
using ChatApp.Contracts.Requests;
using ChatApp.Contracts.Responses;

namespace ChatApp.Client.Application.Services;

public interface IChatApiClient
{
    Task<ChatMessageDto?> SendMessageAsync(SendMessageRequest request, CancellationToken cancellationToken = default);
    Task<GetMessagesResponse?> GetMessagesAsync(DateTime? since = null, Int32 limit = 100, CancellationToken cancellationToken = default);
    Task<GetMessagesResponse?> GetMessagesForNameAsync(Int32 limit = 100, String? senderName = null, CancellationToken cancellationToken = default);
}
