namespace BattlegroundsGallery.Server;

/// <summary>
/// Configuration for the custom HTTP server.
/// Values can be overridden with command-line arguments:
///   --host localhost --port 5080 --webroot wwwroot
/// </summary>
public sealed record HttpServerOptions
{
    public string Host { get; init; } = "localhost";

    public int Port { get; init; } = 5080;

    public string WebRootPath { get; init; } = Path.Combine(AppContext.BaseDirectory, "wwwroot");

    public string Prefix => $"http://{Host}:{Port}/";

    public static HttpServerOptions Parse(string[] args)
    {
        var options = new HttpServerOptions();

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--host" when i + 1 < args.Length:
                    options = options with { Host = args[++i] };
                    break;

                case "--port" when i + 1 < args.Length && int.TryParse(args[++i], out var port):
                    options = options with { Port = port };
                    break;

                case "--webroot" when i + 1 < args.Length:
                    options = options with { WebRootPath = args[++i] };
                    break;
            }
        }

        if (int.TryParse(Environment.GetEnvironmentVariable("PORT"), out var envPort))
        {
            options = options with { Port = envPort };
        }

        if (!Path.IsPathRooted(options.WebRootPath))
        {
            options = options with
            {
                WebRootPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, options.WebRootPath)),
            };
        }

        return options;
    }
}
