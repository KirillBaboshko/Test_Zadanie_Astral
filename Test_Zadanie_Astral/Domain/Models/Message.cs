namespace Test_Zadanie_Astral.Domain.Models;

public sealed class Message
{
    public MessageType Type { get; }
    public String Content { get; }
    public String? SenderName { get; }
    public DateTime Timestamp { get; }

    private Message(MessageType type, String content, String? senderName = null)
    {
        Type = type;
        Content = content ?? String.Empty;
        SenderName = senderName;
        Timestamp = DateTime.Now;
    }

    public static Message Chat(String senderName, String content) =>
        new(MessageType.Chat, content, senderName);

    public static Message System(String content) =>
        new(MessageType.System, content);

    public static Message Join(String name) =>
        new(MessageType.Join, name);

    public static Message Leave() =>
        new(MessageType.Leave, String.Empty);

    public static Message Ok(String content) =>
        new(MessageType.Ok, content);

    public static Message Error(String content) =>
        new(MessageType.Error, content);
}

public enum MessageType
{
    Chat,
    System,
    Join,
    Leave,
    Ok,
    Error
}
