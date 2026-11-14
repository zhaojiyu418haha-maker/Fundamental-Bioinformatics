namespace MiniHttpServer;

public static class MimeTypeResolver
{
    private static readonly IReadOnlyDictionary<string, string> Types =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".html"] = "text/html; charset=utf-8",
            [".htm"] = "text/html; charset=utf-8",
            [".css"] = "text/css; charset=utf-8",
            [".js"] = "text/javascript; charset=utf-8",
            [".json"] = "application/json; charset=utf-8",
            [".png"] = "image/png",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".gif"] = "image/gif",
            [".webp"] = "image/webp",
            [".svg"] = "image/svg+xml",
            [".ico"] = "image/x-icon",
            [".woff"] = "font/woff",
            [".woff2"] = "font/woff2",
            [".ttf"] = "font/ttf",
            [".otf"] = "font/otf",
            [".mp3"] = "audio/mpeg",
            [".mp4"] = "video/mp4",
            [".pdf"] = "application/pdf",
        };

    public static string GetContentType(string path)
    {
        var extension = Path.GetExtension(path);
        return Types.TryGetValue(extension, out var contentType)
            ? contentType
            : "application/octet-stream";
    }
}
