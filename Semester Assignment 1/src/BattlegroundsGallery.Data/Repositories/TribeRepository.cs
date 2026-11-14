using BattlegroundsGallery.Data.Models;
using BattlegroundsGallery.Orm;

namespace BattlegroundsGallery.Data.Repositories;

public sealed class TribeRepository : ITribeRepository
{
    private readonly string _connectionString;

    public TribeRepository(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        _connectionString = connectionString;
    }

    public async Task<IReadOnlyList<Tribe>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        await using var session = new OrmSession(_connectionString);
        return await session.QueryAsync<Tribe>(
            "SELECT * FROM tribes ORDER BY name;",
            cancellationToken: cancellationToken);
    }

    public async Task<Tribe?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        await using var session = new OrmSession(_connectionString);
        return await session.FindAsync<Tribe>(id, cancellationToken);
    }
}
