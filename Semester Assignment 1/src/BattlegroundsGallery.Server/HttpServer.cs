using System.Net;
using BattlegroundsGallery.Server.Exceptions;
using BattlegroundsGallery.Server.Http;
using BattlegroundsGallery.Server.Routing;
using BattlegroundsGallery.Server.StaticFiles;

namespace BattlegroundsGallery.Server;

/// <summary>
/// Custom HTTP server built on <see cref="HttpListener"/>.
/// The server owns the request loop, routing, static files and error handling;
/// it doesn't use ASP.NET Core or Kestrel.
/// </summary>
public sealed class HttpServer : IAsyncDisposable
{
    private readonly HttpServerOptions _options;
    private readonly Router _router;
    private readonly StaticFileMiddleware _staticFiles;
    private readonly Action<string> _log;
    private readonly HttpListener _listener = new();

    public HttpServer(
        HttpServerOptions options,
        Router router,
        StaticFileMiddleware staticFiles,
        Action<string>? logger = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _router = router ?? throw new ArgumentNullException(nameof(router));
        _staticFiles = staticFiles ?? throw new ArgumentNullException(nameof(staticFiles));
        _log = logger ?? Console.WriteLine;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        _listener.Prefixes.Add(_options.Prefix);

        try
        {
            _listener.Start();
        }
        catch (HttpListenerException exception)
        {
            throw new InvalidOperationException(
                $"Could not start HttpListener on '{_options.Prefix}'. " +
                $"Windows returned: {exception.Message}. " +
                "Run scripts\\Setup-UrlAcl.ps1 once as Administrator, " +
                "or start Visual Studio as Administrator.",
                exception);
        }

        _log($"HTTP server listening on {_options.Prefix}");
        _log($"Web root: {_options.WebRootPath}");
        _log("Press Ctrl+C to stop.");

        using var stopRegistration = cancellationToken.Register(StopListener);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                HttpListenerContext context;

                try
                {
                    context = await _listener.GetContextAsync().ConfigureAwait(false);
                }
                catch (HttpListenerException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                _ = ProcessRequestAsync(context);
            }
        }
        finally
        {
            StopListener();
            _listener.Close();
        }
    }

    private async Task ProcessRequestAsync(HttpListenerContext listenerContext)
    {
        var context = new RequestContext(listenerContext);

        try
        {
            if (_router.TryGetHandler(context.Method, context.Path, out var handler))
            {
                await handler(context).ConfigureAwait(false);
                return;
            }

            if (await _staticFiles.TryServeAsync(context).ConfigureAwait(false))
            {
                return;
            }

            await WriteNotFoundAsync(context).ConfigureAwait(false);
        }
        catch (HttpStatusException exception)
        {
            await TryWriteStatusPageAsync(context, exception.StatusCode, exception.Message)
                .ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _log($"[{context.Method} {context.Path}] Unhandled exception: {exception}");
            await TryWriteStatusPageAsync(
                    context,
                    StatusCodes.Status500InternalServerError,
                    "Internal server error")
                .ConfigureAwait(false);
        }
        finally
        {
            try
            {
                listenerContext.Response.Close();
            }
            catch
            {
                // The client may already have disconnected; never let that
                // exception escape the request loop.
            }
        }
    }

    private static Task WriteNotFoundAsync(RequestContext context)
        => context.WriteHtmlAsync(
            """
            <!doctype html>
            <html lang="ru">
            <head><meta charset="utf-8"><title>404</title></head>
            <body style="font-family: system-ui; padding: 3rem">
              <h1>404 — Страница не найдена</h1>
              <p><a href="/">Вернуться на главную</a></p>
            </body>
            </html>
            """,
            StatusCodes.Status404NotFound);

    private async Task TryWriteStatusPageAsync(RequestContext context, int statusCode, string? message)
    {
        try
        {
            if (context.Response.OutputStream.CanWrite)
            {
                await context.WriteHtmlAsync(
                        $"""
                         <!doctype html>
                         <html lang="ru">
                         <head><meta charset="utf-8"><title>{statusCode}</title></head>
                         <body style="font-family: system-ui; padding: 3rem">
                           <h1>{statusCode}</h1>
                           <p>{WebUtility.HtmlEncode(message ?? "Ошибка")}</p>
                           <p><a href="/">Вернуться на главную</a></p>
                         </body>
                         </html>
                         """,
                        statusCode)
                    .ConfigureAwait(false);
            }
        }
        catch (Exception exception)
        {
            _log($"Could not write error response: {exception.Message}");
        }
    }

    private void StopListener()
    {
        try
        {
            if (_listener.IsListening)
            {
                _listener.Stop();
            }
        }
        catch
        {
            // Shutdown must be best-effort.
        }
    }

    public ValueTask DisposeAsync()
    {
        StopListener();
        _listener.Close();
        return ValueTask.CompletedTask;
    }
}
