using ChatApp.Contracts.Messages;

namespace ChatApp.Contracts.Responses;

public sealed class GetMessagesResponse
{
    public List<ChatMessageDto> Messages { get; set; } = new();
    public Int32 TotalCount { get; set; }
}
