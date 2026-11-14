namespace BattlegroundsGallery.Server.StaticFiles;

public static class MimeTypes
{
    private static readonly IReadOnlyDictionary<string, string> Types =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".html"] = "text/html; charset=utf-8",
            [".htm"] = "text/html; charset=utf-8",
            [".css"] = "text/css; charset=utf-8",
            [".js"] = "text/javascript; charset=utf-8",
            [".json"] = "application/json; charset=utf-8",
            [".map"] = "application/json; charset=utf-8",
            [".png"] = "image/png",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".webp"] = "image/webp",
            [".gif"] = "image/gif",
            [".svg"] = "image/svg+xml",
            [".ico"] = "image/x-icon",
            [".woff"] = "font/woff",
            [".woff2"] = "font/woff2",
            [".ttf"] = "font/ttf",
            [".txt"] = "text/plain; charset=utf-8",
        };

    public static string GetContentType(string path)
        => Types.TryGetValue(Path.GetExtension(path), out var contentType)
            ? contentType
            : "application/octet-stream";
}
