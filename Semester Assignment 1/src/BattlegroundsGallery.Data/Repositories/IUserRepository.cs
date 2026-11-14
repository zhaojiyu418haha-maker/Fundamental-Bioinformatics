namespace BattlegroundsGallery.Data.Repositories;

using BattlegroundsGallery.Data.Models;

public interface IUserRepository
{
    Task<User?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default);

    Task<int> CreateAsync(User user, CancellationToken cancellationToken = default);
}
