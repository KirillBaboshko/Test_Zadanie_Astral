using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using static System.Console;

namespace Test_Zadanie_Astral.Models
{
    public class ChatUser
    {
        public String Name { get; set; } = "";
        public IPAddress RemoteAddress { get; set; } = IPAddress.None;
        public Int32 RemotePort { get; set; } = 0;
        public Int32 ListenPort { get; set; } = 0;
        static private Int32 TryReadPort()
        {
            Int32 port = 0;
            if (Int32.TryParse(ReadLine(), out port) && port is >= 1 and <= 65535)
            {
                return port;
            }
            WriteLine("Некорректный порт. Укажите число от 1 до 65535.");
            return port;
        }
        public Boolean TryReadListenPort(String prompt)
        {
            Write(prompt);
            ListenPort = TryReadPort();
            if (ListenPort == 0)
            {
                return false;
            }
            return true;
        }
        public Boolean TryReadRemotePort(String prompt)
        {
            Write(prompt);
            RemotePort = TryReadPort();
            if (RemotePort == 0)
            {
                return false;
            }
            return true;
        }
        public Boolean TryReadName(String prompt)
        {
            Write(prompt);
            Name = ReadLine()?.Trim() ?? String.Empty;
            if (String.IsNullOrWhiteSpace(Name))
            {
                Name = "Anonymous";
            }
            return true;
        }
        public Boolean TryParseRemoteAddress()
        {
            Write("Адрес получателя (IP или host:port): ");
            String? addressInput = ReadLine()?.Trim();
            if (String.IsNullOrWhiteSpace(addressInput))
            {
                WriteLine("Адрес не указан.");
                return false;
            }

            String host = addressInput;
            Int32 port = 0;
            if (addressInput.Contains(':'))
            {
                String[] parts = addressInput.Split(':', 2);
                host = parts[0];
                if (!int.TryParse(parts[1], out port) || port is < 1 or > 65535)
                {
                    WriteLine("Некорректный порт в адресе.");
                    return false;
                }
                RemotePort = port;
            }
            else
            {
                if (!TryReadRemotePort("Порт получателя: "))
                    return false;
            }

            try
            {
                IPAddress[] addresses = Dns.GetHostAddresses(host);
                RemoteAddress = addresses.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork)
                          ?? addresses.FirstOrDefault()
                          ?? IPAddress.None;
            }
            catch (SocketException)
            {
                WriteLine("Не удалось определить адрес получателя.");
                return false;
            }

            if (RemoteAddress.Equals(IPAddress.None))
            {
                WriteLine("Не удалось определить адрес получателя.");
                return false;
            }

            return true;
        }


    }
}
