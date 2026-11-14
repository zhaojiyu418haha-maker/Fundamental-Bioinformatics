namespace BattlegroundsGallery.Data.Repositories;

using BattlegroundsGallery.Data.Models;

public interface IFeedbackRepository
{
    Task<int> CreateAsync(Feedback feedback, CancellationToken cancellationToken = default);
}
