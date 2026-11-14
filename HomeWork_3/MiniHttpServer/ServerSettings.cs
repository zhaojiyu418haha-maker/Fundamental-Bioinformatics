namespace MiniHttpServer;

public sealed class ServerSettings
{
    public string[] Prefixes { get; set; } = Array.Empty<string>();

    public string DefaultPage { get; set; } = "SearcherFinder/index.html";
}
