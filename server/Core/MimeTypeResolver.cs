namespace server.Core;

public static class MimeTypeResolver
{
    private static readonly Dictionary<string, string> _map =
        new(StringComparer.OrdinalIgnoreCase)
    {
        { ".html", "text/html; charset=utf-8" },
        { ".htm",  "text/html; charset=utf-8" },
        { ".css",  "text/css; charset=utf-8" },
        { ".js",   "text/javascript; charset=utf-8" },
        { ".json", "application/json; charset=utf-8" },
        { ".png",  "image/png" },
        { ".jpg",  "image/jpeg" },
        { ".jpeg", "image/jpeg" },
        { ".gif",  "image/gif" },
        { ".webp", "image/webp" },
        { ".svg",  "image/svg+xml" },
        { ".ico",  "image/x-icon" },
        { ".woff", "font/woff" },
        { ".woff2","font/woff2" },
        { ".ttf",  "font/ttf" },
        { ".otf",  "font/otf" },
        { ".mp3",  "audio/mpeg" },
        { ".mp4",  "video/mp4" },
        { ".pdf",  "application/pdf" },
    };

    public static string GetContentType(string extension)
    {
        return _map.TryGetValue(extension ?? "", out var mime)
            ? mime
            : "application/octet-stream";
    }
}