using System.Net;
using System.Text;
using System.Text.Json;

namespace BattlegroundsGallery.Server.Http;

/// <summary>
/// Small wrapper around <see cref="HttpListenerContext"/> used by route handlers.
/// Keeps response-writing details out of the web server loop.
/// </summary>
public sealed class RequestContext
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpListenerContext _context;

    public RequestContext(HttpListenerContext context)
    {
        _context = context;
        Path = NormalizePath(context.Request.Url?.AbsolutePath);
        Query = ParseQuery(context.Request.Url?.Query);
    }

    public string Method => _context.Request.HttpMethod;

    public string Path { get; }

    public IReadOnlyDictionary<string, string> Query { get; }

    public HttpListenerRequest Request => _context.Request;

    public HttpListenerResponse Response => _context.Response;

    public Task WriteHtmlAsync(string html, int statusCode = StatusCodes.Status200OK)
        => WriteTextAsync(html, "text/html; charset=utf-8", statusCode);

    public Task WriteTextAsync(
        string text,
        string contentType = "text/plain; charset=utf-8",
        int statusCode = StatusCodes.Status200OK)
        => WriteBytesAsync(Encoding.UTF8.GetBytes(text), contentType, statusCode);

    public Task WriteJsonAsync<T>(T value, int statusCode = StatusCodes.Status200OK)
        => WriteTextAsync(JsonSerializer.Serialize(value, JsonOptions), "application/json; charset=utf-8", statusCode);

    public async Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> ReadFormValuesAsync(
        CancellationToken cancellationToken = default)
    {
        const int maxBodyBytes = 64 * 1024;

        if (Request.ContentLength64 > maxBodyBytes)
        {
            throw new InvalidDataException("Request body is too large.");
        }

        var contentType = Request.ContentType ?? string.Empty;
        if (!contentType.StartsWith(
                "application/x-www-form-urlencoded",
                StringComparison.OrdinalIgnoreCase))
        {
            return new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        }

        using var reader = new StreamReader(
            Request.InputStream,
            Request.ContentEncoding ?? Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            leaveOpen: true);

        var body = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        return ParseFormValues(body);
    }

    public async Task<IReadOnlyDictionary<string, string>> ReadFormAsync(
        CancellationToken cancellationToken = default)
    {
        var values = await ReadFormValuesAsync(cancellationToken).ConfigureAwait(false);
        return values.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.LastOrDefault() ?? string.Empty,
            StringComparer.OrdinalIgnoreCase);
    }

    public string? GetCookie(string name)
        => Request.Cookies[name]?.Value;

    public void SetCookie(
        string name,
        string value,
        DateTime? expiresUtc = null,
        bool httpOnly = true)
    {
        Response.Cookies.Add(new Cookie(name, value)
        {
            Path = "/",
            HttpOnly = httpOnly,
            Expires = expiresUtc ?? DateTime.UtcNow.AddHours(8),
        });
    }

    public void DeleteCookie(string name)
    {
        Response.Cookies.Add(new Cookie(name, string.Empty)
        {
            Path = "/",
            HttpOnly = true,
            Expires = DateTime.UtcNow.AddDays(-1),
        });
    }

    public Task RedirectAsync(string location, int statusCode = StatusCodes.Status302Found)
    {
        Response.StatusCode = statusCode;
        Response.RedirectLocation = location;
        Response.ContentLength64 = 0;
        return Task.CompletedTask;
    }

    public async Task WriteBytesAsync(byte[] body, string contentType, int statusCode = StatusCodes.Status200OK)
    {
        Response.StatusCode = statusCode;
        Response.ContentType = contentType;
        Response.ContentLength64 = body.Length;
        Response.Headers["X-Content-Type-Options"] = "nosniff";

        if (Method.Equals("HEAD", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        await Response.OutputStream.WriteAsync(body).ConfigureAwait(false);
    }

    public static string NormalizePath(string? rawPath)
    {
        if (string.IsNullOrWhiteSpace(rawPath))
        {
            return "/";
        }

        var path = Uri.UnescapeDataString(rawPath);
        if (!path.StartsWith('/'))
        {
            path = "/" + path;
        }

        return path.Length > 1 ? path.TrimEnd('/') : path;
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<string>> ParseFormValues(string body)
    {
        var values = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var pair in body.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            var key = Uri.UnescapeDataString(parts[0].Replace('+', ' '));
            var value = parts.Length > 1
                ? Uri.UnescapeDataString(parts[1].Replace('+', ' '))
                : string.Empty;

            if (key.Length == 0)
            {
                continue;
            }

            if (!values.TryGetValue(key, out var list))
            {
                list = new List<string>();
                values[key] = list;
            }

            list.Add(value);
        }

        return values.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyList<string>)pair.Value,
            StringComparer.OrdinalIgnoreCase);
    }

    private static IReadOnlyDictionary<string, string> ParseForm(string body)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var pair in body.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            var key = Uri.UnescapeDataString(parts[0].Replace('+', ' '));
            var value = parts.Length > 1
                ? Uri.UnescapeDataString(parts[1].Replace('+', ' '))
                : string.Empty;

            if (key.Length > 0)
            {
                values[key] = value;
            }
        }

        return values;
    }

    private static IReadOnlyDictionary<string, string> ParseQuery(string? query)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(query))
        {
            return values;
        }

        foreach (var pair in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            var key = Uri.UnescapeDataString(parts[0]);
            var value = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : string.Empty;
            values[key] = value;
        }

        return values;
    }
}

/// <summary>
/// Local copy of the most common status codes so the server project does not
/// depend on ASP.NET Core.
/// </summary>
internal static class StatusCodes
{
    public const int Status200OK = 200;
    public const int Status302Found = 302;
    public const int Status404NotFound = 404;
    public const int Status500InternalServerError = 500;
}
