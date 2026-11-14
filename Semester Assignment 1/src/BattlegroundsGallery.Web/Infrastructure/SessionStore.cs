using System.Collections.Concurrent;
using System.Security.Cryptography;
using BattlegroundsGallery.Data.Models;

namespace BattlegroundsGallery.Web.Infrastructure;

public sealed record SessionInfo(
    int UserId,
    string Username,
    string Role,
    DateTime ExpiresAtUtc);

/// <summary>
/// In-memory session store for the local assignment. The browser only gets the
/// random session id; user data stays on the server.
/// </summary>
public sealed class SessionStore
{
    public const string CookieName = "bg_session";

    private static readonly TimeSpan Lifetime = TimeSpan.FromHours(8);
    private readonly ConcurrentDictionary<string, SessionInfo> _sessions =
        new(StringComparer.Ordinal);

    public string Create(User user)
    {
        var sessionId = Convert.ToHexString(RandomNumberGenerator.GetBytes(32))
            .ToLowerInvariant();

        _sessions[sessionId] = new SessionInfo(
            user.Id,
            user.Username,
            user.Role,
            DateTime.UtcNow.Add(Lifetime));

        return sessionId;
    }

    public SessionInfo? Get(string? sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId)
            || !_sessions.TryGetValue(sessionId, out var session))
        {
            return null;
        }

        if (session.ExpiresAtUtc <= DateTime.UtcNow)
        {
            _sessions.TryRemove(sessionId, out _);
            return null;
        }

        return session;
    }

    public void Remove(string? sessionId)
    {
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            _sessions.TryRemove(sessionId, out _);
        }
    }

    public static DateTime GetCookieExpiration() => DateTime.UtcNow.Add(Lifetime);
}
