namespace ChatApp.Server.Domain.Entities;

public sealed class ChatMessage
{
    public Guid Id { get; private set; }
    public String SenderName { get; private set; } = String.Empty;
    public String Content { get; private set; } = String.Empty;
    public DateTime Timestamp { get; private set; }

    private ChatMessage() { } 

    public ChatMessage(String senderName, String content)
    {
        if (String.IsNullOrWhiteSpace(senderName))
            throw new ArgumentException("Имя отправителя не может быть пустым", nameof(senderName));

        if (String.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Содержимое не может быть пустым", nameof(content));

        Id = Guid.NewGuid();
        SenderName = senderName.Trim();
        Content = content.Trim();
        Timestamp = DateTime.UtcNow;
    }
}
