using System.Net;
using System.Text;

namespace server.Core;

public class HttpServer
{
    private readonly HttpListener _listener = new();
    private readonly string[] _prefixes;
    private readonly string _staticRoot;
    private bool _isRunning;

    public HttpServer(string[] prefixes)
    {
        _prefixes = prefixes;
        _staticRoot = Path.Combine(Directory.GetCurrentDirectory(), "static");
    }

    public void Start()
    {
        foreach (var prefix in _prefixes)
            _listener.Prefixes.Add(prefix);

        _listener.Start();
        _isRunning = true;

        Console.WriteLine("Сервер начал свою работу");
        foreach (var p in _prefixes) Console.WriteLine("  " + p);

        // 启动无限循环监听
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
        Console.WriteLine("Сервер завершил свою работу");
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
            catch (HttpListenerException) { break; }
            catch (ObjectDisposedException) { break; }

            _ = ProcessRequestAsync(context);
        }
    }

    private async Task ProcessRequestAsync(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        string path = request.Url?.LocalPath ?? "/";

        // 去掉 /connection 前缀（因为前缀是 http://127.0.0.1:8888/connection/）
        if (path.StartsWith("/connection"))
            path = path.Substring("/connection".Length);

        Console.WriteLine($"Пришел запрос: {path}");

        // 主页 → search-engine.html
        if (string.IsNullOrEmpty(path) || path == "/")
            path = "/search-engine.html";

        string relative = path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        string filePath = Path.Combine(_staticRoot, relative);

        if (!File.Exists(filePath))
        {
            response.StatusCode = 404;
            filePath = Path.Combine(_staticRoot, "404.html");

            if (!File.Exists(filePath))
            {
                byte[] err = Encoding.UTF8.GetBytes("<h1>404 Not Found</h1>");
                response.ContentType = "text/html; charset=utf-8";
                response.ContentLength64 = err.Length;
                await response.OutputStream.WriteAsync(err);
                response.Close();
                return;
            }
        }

        // Content-Type 交给独立类
        response.ContentType = MimeTypeResolver.GetContentType(Path.GetExtension(filePath));

        byte[] buffer = await File.ReadAllBytesAsync(filePath);
        response.ContentLength64 = buffer.Length;

        using (Stream output = response.OutputStream)
        {
            await output.WriteAsync(buffer);
            await output.FlushAsync();
        }

        response.Close();
        Console.WriteLine("Запрос обработан");
    }
}