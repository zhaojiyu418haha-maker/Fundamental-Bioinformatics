using BattlegroundsGallery.Server.Http;

namespace BattlegroundsGallery.Server.Routing;

/// <summary>
/// Minimal route table for the custom server. Routes are separated by HTTP
/// method; path parameters are intentionally not implemented yet.
/// </summary>
public sealed class Router
{
    private readonly Dictionary<string, Func<RequestContext, Task>> _getRoutes =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, Func<RequestContext, Task>> _postRoutes =
        new(StringComparer.OrdinalIgnoreCase);

    public void MapGet(string path, Func<RequestContext, Task> handler)
        => Map(_getRoutes, path, handler);

    public void MapPost(string path, Func<RequestContext, Task> handler)
        => Map(_postRoutes, path, handler);

    public bool TryGetHandler(
        string method,
        string path,
        out Func<RequestContext, Task> handler)
    {
        var routes = method.Equals("POST", StringComparison.OrdinalIgnoreCase)
            ? _postRoutes
            : _getRoutes;

        return routes.TryGetValue(RequestContext.NormalizePath(path), out handler!);
    }

    private static void Map(
        IDictionary<string, Func<RequestContext, Task>> routes,
        string path,
        Func<RequestContext, Task> handler)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(handler);

        routes[RequestContext.NormalizePath(path)] = handler;
    }
}
