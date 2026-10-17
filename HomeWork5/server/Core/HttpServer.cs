using System.Collections.Specialized;
using System.Net;
using System.Reflection;
using System.Text;
using System.Web;
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
        _staticRoot = Path.Combine(AppContext.BaseDirectory, "static");
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
        Console.WriteLine($"Пришел запрос: {request.HttpMethod} {path}");

        // ============ 第一部分：处理 POST 请求（表单提交，走反射路由） ============
        if (request.HttpMethod == "POST")
        {
            // 1. 读取表单数据
            using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
            string body = await reader.ReadToEndAsync();

            var formData = new Dictionary<string, string>();
            foreach (var pair in body.Split('&'))
            {
                var kv = pair.Split('=');
                if (kv.Length == 2)
                    formData[Uri.UnescapeDataString(kv[0])] = Uri.UnescapeDataString(kv[1]);
            }

            // 2. 从 URL 里取控制器名（URL 格式：/Account/Login → 控制器名 = "Account"）
            string[] segments = request.Url.Segments;
            string controllerName = segments.Length > 1 ? segments[1].Replace("/", "") : "";

            // 3. 反射查找控制器
            var assembly = Assembly.GetExecutingAssembly();
            var controller = assembly.GetTypes()
                .Where(t => Attribute.IsDefined(t, typeof(HttpController)))
                .FirstOrDefault(c => c.Name.ToLower() == controllerName.ToLower());

            if (controller != null)
            {
                // 4. 找到带 [HttpPost] 的方法
                // 4. 找到带 [HttpXxx] 的方法（忽略大小写：HttpPost 与 "Http"+"POST" 视为同一特性）
                string verbAttrName = $"Http{request.HttpMethod}";
                string actionName = segments.Length > 2 ? segments[2].Replace("/", "") : "";

                var candidates = controller.GetMethods()
                    .Where(t => t.GetCustomAttributes(true)
                        .Any(attr => string.Equals(attr.GetType().Name, verbAttrName, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                // 优先按 URL 里的动作名（Register / Login / Forget）匹配，匹配不到再退回第一个
                var method = candidates
                    .FirstOrDefault(m => m.Name.Equals(actionName, StringComparison.OrdinalIgnoreCase))
                    ?? candidates.FirstOrDefault();

                if (method != null)
                {
                    // 5. 按参数名从表单数据里取值
                    object[] queryParams = method.GetParameters()
                        .Select(p => formData.ContainsKey(p.Name)
                            ? Convert.ChangeType(formData[p.Name], p.ParameterType)
                            : null)
                        .ToArray();

                    // 6. 执行方法，拿到返回的 HTML
                    var ret = method.Invoke(Activator.CreateInstance(controller), queryParams);

                    if (ret is string htmlResult)
                    {
                        byte[] htmlBytes = Encoding.UTF8.GetBytes(htmlResult);
                        response.ContentType = "text/html; charset=utf-8";
                        response.ContentLength64 = htmlBytes.Length;
                        await response.OutputStream.WriteAsync(htmlBytes);
                        response.Close();
                        return; // 处理完直接返回，不走静态文件逻辑
                    }
                }
            }

            // 如果 POST 没匹配到控制器，返回 404
            response.StatusCode = 404;
            byte[] errBytes = Encoding.UTF8.GetBytes("<h1>404 - 控制器或方法未找到</h1>");
            response.ContentType = "text/html; charset=utf-8";
            response.ContentLength64 = errBytes.Length;
            await response.OutputStream.WriteAsync(errBytes);
            response.Close();
            return;
        }

        // ============ 第二部分：处理 GET 请求（返回静态 HTML 文件） ============
        // 主页 → web.html
        if (path == "/" || path == "")
            path = "/web.html";

        // 如果访问的是 /login 或 /register 或 /forget（不带 .html），自动补上 .html
        if (!Path.HasExtension(path))
            path += ".html";

        string relative = path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        string filePath = Path.Combine(_staticRoot, relative);

        // 安全检查：不能跳出 static 目录
        if (!filePath.StartsWith(_staticRoot, StringComparison.OrdinalIgnoreCase))
        {
            response.StatusCode = 403;
            response.Close();
            return;
        }

        // 文件不存在 → 404
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

        // 返回静态文件
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