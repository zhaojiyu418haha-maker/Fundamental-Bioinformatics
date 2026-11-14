using BattlegroundsGallery.Data.Models;
using BattlegroundsGallery.Orm;
using Npgsql;

namespace BattlegroundsGallery.Data.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly string _connectionString;

    public UserRepository(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        _connectionString = connectionString;
    }

    public async Task<User?> FindByUsernameAsync(
        string username,
        CancellationToken cancellationToken = default)
    {
        await using var session = new OrmSession(_connectionString);
        var users = await session.QueryAsync<User>(
            "SELECT * FROM users WHERE username = @username LIMIT 1;",
            [new NpgsqlParameter("username", username)],
            cancellationToken);

        return users.FirstOrDefault();
    }

    public async Task<int> CreateAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        await using var session = new OrmSession(_connectionString);
        return await session.InsertAsync(user, cancellationToken);
    }
}
