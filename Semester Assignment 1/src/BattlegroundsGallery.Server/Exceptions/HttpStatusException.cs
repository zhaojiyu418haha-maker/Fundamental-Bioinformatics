namespace BattlegroundsGallery.Server.Exceptions;

/// <summary>
/// Thrown when a request should be completed with a specific HTTP status code
/// without taking down the server loop.
/// </summary>
public sealed class HttpStatusException : Exception
{
    public HttpStatusException(int statusCode, string? message = null)
        : base(message ?? $"HTTP {statusCode}")
    {
        StatusCode = statusCode;
    }

    public int StatusCode { get; }
}
