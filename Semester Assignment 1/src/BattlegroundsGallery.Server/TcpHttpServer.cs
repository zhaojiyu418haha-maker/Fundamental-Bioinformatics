using System.Net;
using System.Net.Sockets;
using System.Text;
using BattlegroundsGallery.Server.StaticFiles;

namespace BattlegroundsGallery.Server;

/// <summary>
/// Compatibility HTTP/1.1 server used only when the Windows HttpListener API is
/// unavailable on the current machine. The primary server remains
/// <see cref="HttpServer"/>; this class exists so the static gallery can still
/// be opened for development and demonstration.
/// </summary>
public sealed class TcpHttpServer : IAsyncDisposable
{
    private static readonly byte[] NotFoundBody = Encoding.UTF8.GetBytes(
        """
        <!doctype html>
        <html lang="zh-CN">
        <head><meta charset="utf-8"><title>404</title></head>
        <body style="font-family: system-ui; padding: 3rem">
          <h1>404 — 页面不存在</h1>
          <p><a href="/">返回首页</a></p>
        </body>
        </html>
        """);

    private readonly HttpServerOptions _options;
    private readonly string _webRootPath;
    private readonly string _webRootWithSeparator;
    private readonly Action<string> _log;
    private TcpListener? _listener;

    public TcpHttpServer(HttpServerOptions options, Action<string>? logger = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _webRootPath = Path.GetFullPath(options.WebRootPath);
        _webRootWithSeparator = _webRootPath.TrimEnd(
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        _log = logger ?? Console.WriteLine;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        _listener = new TcpListener(IPAddress.Loopback, _options.Port);
        _listener.Start();

        _log($"HttpListener is unavailable; compatibility HTTP server listening on http://localhost:{_options.Port}/");
        _log("Press Ctrl+C to stop.");

        using var stopRegistration = cancellationToken.Register(StopListener);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                TcpClient client;

                try
                {
                    client = await _listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (SocketException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                _ = HandleClientAsync(client);
            }
        }
        finally
        {
            StopListener();
        }
    }

    private async Task HandleClientAsync(TcpClient client)
    {
        using (client)
        {
            try
            {
                client.ReceiveTimeout = 10_000;
                client.SendTimeout = 10_000;

                await using var stream = client.GetStream();
                var request = await ReadRequestAsync(stream).ConfigureAwait(false);
                if (request is null)
                {
                    return;
                }

                var (method, target) = request.Value;
                var path = GetPath(target);
                await WriteResponseAsync(stream, method, path).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is IOException or SocketException or ObjectDisposedException)
            {
                // A browser may close a connection while assets are loading.
            }
            catch (Exception exception)
            {
                _log($"Compatibility server request failed: {exception}");
            }
        }
    }

    private async Task WriteResponseAsync(NetworkStream stream, string method, string path)
    {
        var statusCode = 200;
        var reason = "OK";
        var contentType = "text/html; charset=utf-8";
        byte[] body;

        if (path.Equals("/health", StringComparison.OrdinalIgnoreCase))
        {
            contentType = "application/json; charset=utf-8";
            body = Encoding.UTF8.GetBytes(
                $$"""{"status":"ok","server":"TcpListener compatibility mode","timestampUtc":"{{DateTime.UtcNow:O}}"}""");
        }
        else if (path.Equals("/api/status", StringComparison.OrdinalIgnoreCase))
        {
            contentType = "application/json; charset=utf-8";
            body = Encoding.UTF8.GetBytes(
                """{"project":"BattlegroundsGallery","stage":"server-skeleton","database":"not configured yet"}""");
        }
        else if (TryResolveFile(path, out var filePath))
        {
            contentType = MimeTypes.GetContentType(filePath);
            body = await File.ReadAllBytesAsync(filePath).ConfigureAwait(false);
        }
        else
        {
            statusCode = 404;
            reason = "Not Found";
            body = NotFoundBody;
        }

        var header = $"HTTP/1.1 {statusCode} {reason}\r\n" +
                     $"Content-Type: {contentType}\r\n" +
                     $"Content-Length: {body.Length}\r\n" +
                     "Cache-Control: no-cache\r\n" +
                     "Connection: close\r\n" +
                     "X-Content-Type-Options: nosniff\r\n\r\n";

        await stream.WriteAsync(Encoding.ASCII.GetBytes(header)).ConfigureAwait(false);

        if (!method.Equals("HEAD", StringComparison.OrdinalIgnoreCase))
        {
            await stream.WriteAsync(body).ConfigureAwait(false);
        }
    }

    private bool TryResolveFile(string path, out string filePath)
    {
        var requestPath = path == "/" ? "/index.html" : path;
        var relativePath = requestPath.TrimStart('/')
            .Replace('/', Path.DirectorySeparatorChar);

        filePath = Path.GetFullPath(Path.Combine(_webRootPath, relativePath));
        return filePath.StartsWith(_webRootWithSeparator, StringComparison.OrdinalIgnoreCase)
               && File.Exists(filePath);
    }

    private static string GetPath(string target)
    {
        if (Uri.TryCreate("http://localhost" + target, UriKind.Absolute, out var uri))
        {
            var path = Uri.UnescapeDataString(uri.AbsolutePath);
            return path.Length > 1 ? path.TrimEnd('/') : path;
        }

        return "/";
    }

    private static async Task<(string Method, string Target)?> ReadRequestAsync(NetworkStream stream)
    {
        var buffer = new byte[16 * 1024];
        var received = 0;

        while (received < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(received, buffer.Length - received))
                .ConfigureAwait(false);

            if (read == 0)
            {
                break;
            }

            received += read;
            var text = Encoding.ASCII.GetString(buffer, 0, received);
            var headerEnd = text.IndexOf("\r\n\r\n", StringComparison.Ordinal);
            if (headerEnd >= 0)
            {
                var requestLine = text[..headerEnd].Split("\r\n", 2)[0].Split(' ');
                if (requestLine.Length >= 2)
                {
                    return (requestLine[0], requestLine[1]);
                }

                return null;
            }
        }

        return null;
    }

    private void StopListener()
    {
        try
        {
            _listener?.Stop();
        }
        catch
        {
            // Shutdown is best effort.
        }
    }

    public ValueTask DisposeAsync()
    {
        StopListener();
        return ValueTask.CompletedTask;
    }
}
