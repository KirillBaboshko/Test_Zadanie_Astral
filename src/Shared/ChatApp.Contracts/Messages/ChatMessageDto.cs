namespace ChatApp.Contracts.Messages;

public sealed class ChatMessageDto
{
    public Guid Id { get; set; }
    public String SenderName { get; set; } = String.Empty;
    public String Content { get; set; } = String.Empty;
    public DateTime Timestamp { get; set; }
}
