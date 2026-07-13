using System.Net;
using System.Net.Sockets;
using static System.Console;

namespace Test_Zadanie_Astral.Presentation;


public static class ConsoleUI
{
    public static ApplicationMode? SelectMode()
    {
        WriteLine("Выберите режим:");
        WriteLine("1 — Сервер");
        WriteLine("2 — Клиент");
        Write("> ");

        String? input = ReadLine()?.Trim();
        return input switch
        {
            "1" or "server" or "сервер" => ApplicationMode.Server,
            "2" or "client" or "клиент" => ApplicationMode.Client,
            _ => null
        };
    }

    public static Int32? ReadPort(String prompt)
    {
        Write(prompt);
        if (Int32.TryParse(ReadLine(), out Int32 port) && port is >= 1 and <= 65535)
            return port;

        WriteLine("Некорректный порт. Укажите число от 1 до 65535.");
        return null;
    }

    public static String ReadName(String prompt)
    {
        Write(prompt);
        String? name = ReadLine()?.Trim();
        return String.IsNullOrWhiteSpace(name) ? "Anonymous" : name;
    }

    public static (IPAddress? Address, Int32 Port) ReadServerAddress()
    {
        Write("Адрес сервера (IP или host:port): ");
        String? addressInput = ReadLine()?.Trim();

        if (String.IsNullOrWhiteSpace(addressInput))
        {
            WriteLine("Адрес не указан.");
            return (null, 0);
        }

        String host = addressInput;
        Int32 port = 0;

        if (addressInput.Contains(':'))
        {
            String[] parts = addressInput.Split(':', 2);
            host = parts[0];

            if (!Int32.TryParse(parts[1], out port) || port is < 1 or > 65535)
            {
                WriteLine("Некорректный порт в адресе.");
                return (null, 0);
            }
        }
        else
        {
            Int32? readPort = ReadPort("Порт сервера: ");
            if (!readPort.HasValue)
                return (null, 0);
            port = readPort.Value;
        }

        try
        {
            IPAddress[] addresses = Dns.GetHostAddresses(host);
            IPAddress? address = addresses.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork)
                              ?? addresses.FirstOrDefault();

            if (address is null)
            {
                WriteLine("Не удалось определить адрес сервера.");
                return (null, 0);
            }

            return (address, port);
        }
        catch (SocketException)
        {
            WriteLine("Не удалось определить адрес сервера.");
            return (null, 0);
        }
    }
}

public enum ApplicationMode
{
    Server,
    Client
}
