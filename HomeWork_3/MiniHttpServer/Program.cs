using System.Text;
using MiniHttpServer;

Console.OutputEncoding = Encoding.UTF8;

var settingsPath = Path.Combine(AppContext.BaseDirectory, "settings.json");
var settings = ConfigurationManager.Load(settingsPath);

var server = new HttpServer(settings);
server.Start();

Console.WriteLine("Enter 'stop' to stop the server.");

while (true)
{
    var command = Console.ReadLine();
    if (string.Equals(command?.Trim(), "stop", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }
}

server.Stop();
Console.WriteLine("Application finished.");
