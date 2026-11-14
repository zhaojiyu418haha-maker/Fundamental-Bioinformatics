namespace BattlegroundsGallery.Data.Repositories;

using BattlegroundsGallery.Data.Models;

public interface ITribeRepository
{
    Task<IReadOnlyList<Tribe>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Tribe?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}
