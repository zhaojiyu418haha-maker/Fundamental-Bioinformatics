using BattlegroundsGallery.Data.Models;
using BattlegroundsGallery.Orm;

namespace BattlegroundsGallery.Data.Repositories;

public sealed class FeedbackRepository : IFeedbackRepository
{
    private readonly string _connectionString;

    public FeedbackRepository(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        _connectionString = connectionString;
    }

    public async Task<int> CreateAsync(
        Feedback feedback,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(feedback);

        await using var session = new OrmSession(_connectionString);
        return await session.InsertAsync(feedback, cancellationToken);
    }
}
