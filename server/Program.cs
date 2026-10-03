using server.Core;                  // ← 改成 server.Core
using System.Text.Json;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var settings = JsonSerializer.Deserialize<Settings>(
    File.ReadAllText("settings.json"),
    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

var server = new HttpServer(settings.Prefixes);
server.Start();

Console.WriteLine("Введите 'stop' для остановки сервера.");
while (Console.ReadLine()?.Trim().ToLower() != "stop") { }

server.Stop();
Console.ReadKey();