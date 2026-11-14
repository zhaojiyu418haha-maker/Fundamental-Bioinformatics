using BattlegroundsGallery.Data.Models;
using BattlegroundsGallery.Data.Repositories;
using BattlegroundsGallery.Data.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BattlegroundsGallery.UnitTests;

[TestClass]
public sealed class DataTests
{
    [TestMethod]
    public void PasswordHasherVerifiesCorrectPassword()
    {
        var hash = PasswordHasher.Hash("admin123");

        Assert.IsTrue(PasswordHasher.Verify("admin123", hash));
        Assert.IsFalse(PasswordHasher.Verify("wrong-password", hash));
        Assert.IsFalse(PasswordHasher.Verify("admin123", "not-a-valid-hash"));
    }

    [TestMethod]
    public async Task CardRepositoryPerformsCrud()
    {
        var repository = new CardRepository(
            BattlegroundsGallery.Data.DatabaseOptions.DefaultConnectionString);

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var card = new Card
        {
            Slug = $"unit-test-{suffix}",
            Name = $"UnitTest {suffix}",
            Description = "unit test card",
            Attack = 1,
            Health = 1,
            Tier = 1,
            ImagePath = "assets/cards/bubble-gunner.png",
            Tribes = [new Tribe { Id = 8 }],
        };

        var id = 0;

        try
        {
            id = await repository.CreateAsync(card);
            Assert.IsTrue(id > 0);

            var loaded = await repository.GetByIdAsync(id);
            Assert.IsNotNull(loaded);
            Assert.AreEqual(card.Name, loaded.Name);
            Assert.AreEqual(1, loaded.Tribes.Count);

            loaded.Name = $"UnitTest Updated {suffix}";
            loaded.Attack = 4;
            await repository.UpdateAsync(loaded);

            var updated = await repository.GetByIdAsync(id);
            Assert.IsNotNull(updated);
            Assert.AreEqual(4, updated.Attack);

            var search = await repository.SearchAsync(suffix);
            Assert.IsTrue(search.Any(item => item.Id == id));
        }
        finally
        {
            if (id > 0)
            {
                await repository.DeleteAsync(id);
            }
        }

        Assert.IsNull(await repository.GetByIdAsync(id));
    }
}
