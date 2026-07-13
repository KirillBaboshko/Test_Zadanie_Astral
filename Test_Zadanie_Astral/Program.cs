using System.Text;
using Test_Zadanie_Astral.Application;
using static System.Console;

Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

WriteLine("=== UDP Чат (Клиент-Сервер) ===");
WriteLine();

using CancellationTokenSource cts = new();


Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

await Application.RunAsync(cts);
