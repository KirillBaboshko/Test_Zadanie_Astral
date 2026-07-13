using Test_Zadanie_Astral.Domain.Models;

namespace Test_Zadanie_Astral.Domain.Interfaces;


public interface IProtocolSerializer
{
    Byte[] Serialize(Message message);
    Boolean TryDeserialize(Byte[] data, out Message? message);
}
