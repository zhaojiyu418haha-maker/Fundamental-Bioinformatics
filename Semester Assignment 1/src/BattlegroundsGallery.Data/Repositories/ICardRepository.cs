namespace BattlegroundsGallery.Data.Repositories;

using BattlegroundsGallery.Data.Models;

/// <summary>
/// DAO/Repository contract for cards. The implementation will use the custom
/// ORM and will be added after the database schema is agreed on.
/// </summary>
public interface ICardRepository
{
    Task<IReadOnlyList<Card>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Card?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> SlugExistsAsync(
        string slug,
        int? excludeCardId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Card>> SearchAsync(string keyword, CancellationToken cancellationToken = default);

    Task<int> CreateAsync(Card card, CancellationToken cancellationToken = default);

    Task UpdateAsync(Card card, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
