using BattlegroundsGallery.Server.Http;

namespace BattlegroundsGallery.Server.StaticFiles;

/// <summary>
/// Serves files below a single web-root directory. Path traversal is rejected
/// by resolving the request path and checking that it stays below the root.
/// </summary>
public sealed class StaticFileMiddleware
{
    private readonly string _webRootPath;
    private readonly string _webRootWithSeparator;

    public StaticFileMiddleware(string webRootPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(webRootPath);

        _webRootPath = Path.GetFullPath(webRootPath);
        _webRootWithSeparator = _webRootPath.TrimEnd(
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
    }

    public async Task<bool> TryServeAsync(RequestContext context, CancellationToken cancellationToken = default)
    {
        if (!HttpMethod.IsGetOrHead(context.Method))
        {
            return false;
        }

        var requestPath = context.Path == "/" ? "/index.html" : context.Path;
        var relativePath = requestPath.TrimStart('/')
            .Replace('/', Path.DirectorySeparatorChar);

        var filePath = Path.GetFullPath(Path.Combine(_webRootPath, relativePath));
        if (!filePath.StartsWith(_webRootWithSeparator, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!File.Exists(filePath))
        {
            return false;
        }

        var fileInfo = new FileInfo(filePath);
        context.Response.StatusCode = 200;
        context.Response.ContentType = MimeTypes.GetContentType(filePath);
        context.Response.ContentLength64 = fileInfo.Length;
        context.Response.Headers["Cache-Control"] = "no-cache";
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";

        if (!HttpMethod.IsHead(context.Method))
        {
            await using var stream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 64 * 1024,
                useAsync: true);

            await stream.CopyToAsync(context.Response.OutputStream, cancellationToken)
                .ConfigureAwait(false);
        }

        return true;
    }

    private static class HttpMethod
    {
        public static bool IsGetOrHead(string method)
            => IsGet(method) || IsHead(method);

        public static bool IsGet(string method)
            => method.Equals("GET", StringComparison.OrdinalIgnoreCase);

        public static bool IsHead(string method)
            => method.Equals("HEAD", StringComparison.OrdinalIgnoreCase);
    }
}
