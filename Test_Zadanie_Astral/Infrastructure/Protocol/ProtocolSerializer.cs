using System.Text;
using Test_Zadanie_Astral.Domain.Interfaces;
using Test_Zadanie_Astral.Domain.Models;

namespace Test_Zadanie_Astral.Infrastructure.Protocol;


public sealed class ProtocolSerializer : IProtocolSerializer
{
    private const Char Separator = '|';

    public Byte[] Serialize(Message message)
    {
        String encoded = message.Type switch
        {
            MessageType.Join => $"JOIN{Separator}{message.Content}",
            MessageType.Leave => "LEAVE",
            MessageType.Chat => $"CHAT{Separator}{message.SenderName ?? "Unknown"}: {message.Content}",
            MessageType.System => $"SYS{Separator}{message.Content}",
            MessageType.Ok => $"OK{Separator}{message.Content}",
            MessageType.Error => $"ERR{Separator}{message.Content}",
            _ => throw new ArgumentException($"Неизвестный тип сообщения: {message.Type}")
        };

        return Encoding.UTF8.GetBytes(encoded);
    }

    public Boolean TryDeserialize(Byte[] data, out Message? message)
    {
        message = null;

        try
        {
            String text = Encoding.UTF8.GetString(data);
            return TryParse(text, out message);
        }
        catch
        {
            return false;
        }
    }

    private Boolean TryParse(String text, out Message? message)
    {
        message = null;

        Int32 separatorIndex = text.IndexOf(Separator);
        String type;
        String payload;

        if (separatorIndex < 0)
        {
            type = text;
            payload = String.Empty;
        }
        else
        {
            type = text[..separatorIndex];
            payload = text[(separatorIndex + 1)..];
        }

        message = type switch
        {
            "JOIN" => Message.Join(payload),
            "LEAVE" => Message.Leave(),
            "MSG" => Message.Chat(String.Empty, payload), 
            "CHAT" => ParseChatMessage(payload),
            "SYS" => Message.System(payload),
            "OK" => Message.Ok(payload),
            "ERR" => Message.Error(payload),
            _ => null
        };

        return message is not null;
    }

    private Message ParseChatMessage(String payload)
    {
        Int32 colonIndex = payload.IndexOf(':');
        if (colonIndex > 0)
        {
            String name = payload[..colonIndex].Trim();
            String content = payload[(colonIndex + 1)..].Trim();
            return Message.Chat(name, content);
        }

        return Message.System(payload);
    }
}
