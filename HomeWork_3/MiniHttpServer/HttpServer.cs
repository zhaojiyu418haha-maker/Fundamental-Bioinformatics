using System.Net;
using System.Text;

namespace MiniHttpServer;

public sealed class HttpServer
{
    private readonly HttpListener _listener = new();
    private readonly ServerSettings _settings;
    private readonly string _staticRoot;
    private readonly string _staticRootWithSeparator;
    private bool _isRunning;

    public HttpServer(ServerSettings settings)
    {
        _settings = settings;
        _staticRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "static"));
        _staticRootWithSeparator = _staticRoot.TrimEnd(
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
    }

    public void Start()
    {
        foreach (var prefix in _settings.Prefixes)
        {
            _listener.Prefixes.Add(prefix);
        }

        _listener.Start();
        _isRunning = true;

        Console.WriteLine("HTTP server started.");
        foreach (var prefix in _settings.Prefixes)
        {
            Console.WriteLine($"  {prefix}");
        }

        _ = ListenLoopAsync();
    }

    public void Stop()
    {
        _isRunning = false;

        if (_listener.IsListening)
        {
            _listener.Stop();
            _listener.Close();
        }

        Console.WriteLine("HTTP server stopped.");
    }

    private async Task ListenLoopAsync()
    {
        while (_isRunning)
        {
            HttpListenerContext context;

            try
            {
                context = await _listener.GetContextAsync();
            }
            catch (HttpListenerException) when (!_isRunning)
            {
                break;
            }
            catch (ObjectDisposedException) when (!_isRunning)
            {
                break;
            }
            catch (Exception exception)
            {
                Console.WriteLine($"[ERROR] Listener: {exception.Message}");
                continue;
            }

            _ = ProcessRequestAsync(context);
        }
    }

    private async Task ProcessRequestAsync(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        try
        {
            var path = request.Url?.LocalPath ?? "/";
            Console.WriteLine($"Request: {request.HttpMethod} {path}");

            if (request.HttpMethod is not ("GET" or "HEAD"))
            {
                response.StatusCode = 405;
                await WriteTextAsync(response, "405 Method Not Allowed");
                return;
            }

            if (path == "/")
            {
                path = "/" + _settings.DefaultPage.TrimStart('/');
            }

            if (path.EndsWith('/'))
            {
                path += "index.html";
            }

            var relativePath = path.TrimStart('/')
                .Replace('/', Path.DirectorySeparatorChar);
            var filePath = Path.GetFullPath(Path.Combine(_staticRoot, relativePath));

            if (!filePath.StartsWith(_staticRootWithSeparator, StringComparison.OrdinalIgnoreCase))
            {
                response.StatusCode = 403;
                await WriteTextAsync(response, "403 Forbidden");
                return;
            }

            if (!File.Exists(filePath))
            {
                await WriteNotFoundAsync(response);
                return;
            }

            await WriteFileAsync(response, filePath, request.HttpMethod == "HEAD");
        }
        catch (Exception exception)
        {
            Console.WriteLine($"[ERROR] Request: {exception}");
            try
            {
                if (response.OutputStream.CanWrite)
                {
                    response.StatusCode = 500;
                    await WriteTextAsync(response, "500 Internal Server Error");
                }
            }
            catch
            {
                // Client may already be disconnected.
            }
        }
        finally
        {
            try
            {
                response.Close();
            }
            catch
            {
                // Ignore disconnected clients.
            }
        }
    }

    private async Task WriteFileAsync(
        HttpListenerResponse response,
        string filePath,
        bool headOnly)
    {
        var fileInfo = new FileInfo(filePath);
        response.StatusCode = 200;
        response.ContentType = MimeTypeResolver.GetContentType(filePath);
        response.ContentLength64 = fileInfo.Length;
        response.Headers["Cache-Control"] = "no-cache";

        if (headOnly)
        {
            return;
        }

        await using var file = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 64 * 1024,
            useAsync: true);

        await file.CopyToAsync(response.OutputStream);
        Console.WriteLine($"200 OK: {filePath}");
    }

    private async Task WriteNotFoundAsync(HttpListenerResponse response)
    {
        response.StatusCode = 404;

        var notFoundPath = Path.Combine(_staticRoot, "404.html");
        if (File.Exists(notFoundPath))
        {
            response.ContentType = MimeTypeResolver.GetContentType(notFoundPath);
            var bytes = await File.ReadAllBytesAsync(notFoundPath);
            response.ContentLength64 = bytes.Length;
            await response.OutputStream.WriteAsync(bytes);
            Console.WriteLine("404 Not Found: static/404.html");
            return;
        }

        response.ContentType = "text/html; charset=utf-8";
        var fallback = Encoding.UTF8.GetBytes(
            "<!doctype html><html lang=\"ru\"><head><meta charset=\"utf-8\">" +
            "<title>404</title></head><body><h1>404 — Страница не найдена</h1>" +
            "<p><a href=\"/\">На главную</a></p></body></html>");
        response.ContentLength64 = fallback.Length;
        await response.OutputStream.WriteAsync(fallback);
        Console.WriteLine("404 Not Found: built-in page");
    }

    private static async Task WriteTextAsync(
        HttpListenerResponse response,
        string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        response.ContentType = "text/plain; charset=utf-8";
        response.ContentLength64 = bytes.Length;
        await response.OutputStream.WriteAsync(bytes);
    }
}
