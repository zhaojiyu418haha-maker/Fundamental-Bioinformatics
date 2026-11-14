using BattlegroundsGallery.Data.Models;
using BattlegroundsGallery.Orm;
using Npgsql;

namespace BattlegroundsGallery.Data.Repositories;

public sealed class CardRepository : ICardRepository
{
    private readonly string _connectionString;

    public CardRepository(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        _connectionString = connectionString;
    }

    public async Task<IReadOnlyList<Card>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        await using var session = new OrmSession(_connectionString);

        var cards = await session.QueryAsync<Card>(
            "SELECT * FROM cards ORDER BY tier, name;",
            cancellationToken: cancellationToken);

        await LoadTribesAsync(session, cards, cancellationToken);
        return cards;
    }

    public async Task<Card?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        await using var session = new OrmSession(_connectionString);

        var card = await session.FindAsync<Card>(id, cancellationToken);
        if (card is null)
        {
            return null;
        }

        await LoadTribesAsync(session, [card], cancellationToken);
        return card;
    }

    public async Task<bool> SlugExistsAsync(
        string slug,
        int? excludeCardId = null,
        CancellationToken cancellationToken = default)
    {
        await using var session = new OrmSession(_connectionString);

        var rows = await session.QueryAsync<Card>(
            """
            SELECT *
            FROM cards
            WHERE slug = @slug
              AND (@exclude_id = 0 OR id <> @exclude_id)
            LIMIT 1;
            """,
            [
                new NpgsqlParameter("slug", slug),
                new NpgsqlParameter("exclude_id", excludeCardId ?? 0),
            ],
            cancellationToken);

        return rows.Count > 0;
    }

    public async Task<IReadOnlyList<Card>> SearchAsync(
        string keyword,
        CancellationToken cancellationToken = default)
    {
        keyword = keyword.Trim();

        if (keyword.Length == 0)
        {
            return await GetAllAsync(cancellationToken);
        }

        await using var session = new OrmSession(_connectionString);
        var pattern = $"%{keyword}%";

        var cards = await session.QueryAsync<Card>(
            """
            SELECT *
            FROM cards
            WHERE name ILIKE @pattern
               OR description ILIKE @pattern
            ORDER BY tier, name
            LIMIT 200;
            """,
            [new NpgsqlParameter("pattern", pattern)],
            cancellationToken);

        await LoadTribesAsync(session, cards, cancellationToken);
        return cards;
    }

    public async Task<int> CreateAsync(
        Card card,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(card);

        await using var session = new OrmSession(_connectionString);
        await session.BeginTransactionAsync(cancellationToken);

        try
        {
            var id = await session.InsertAsync(card, cancellationToken);
            await ReplaceTribesAsync(session, id, card.Tribes, cancellationToken);
            await session.CommitAsync(cancellationToken);
            return id;
        }
        catch
        {
            await session.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task UpdateAsync(
        Card card,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(card);

        await using var session = new OrmSession(_connectionString);
        await session.BeginTransactionAsync(cancellationToken);

        try
        {
            await session.UpdateAsync(card, cancellationToken);
            await ReplaceTribesAsync(session, card.Id, card.Tribes, cancellationToken);
            await session.CommitAsync(cancellationToken);
        }
        catch
        {
            await session.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task DeleteAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        await using var session = new OrmSession(_connectionString);
        await session.DeleteAsync<Card>(id, cancellationToken);
    }

    private static async Task ReplaceTribesAsync(
        OrmSession session,
        int cardId,
        IEnumerable<Tribe> tribes,
        CancellationToken cancellationToken)
    {
        await session.ExecuteAsync(
            "DELETE FROM cards_tribes WHERE card_id = @card_id;",
            [new NpgsqlParameter("card_id", cardId)],
            cancellationToken);

        var primary = true;

        foreach (var tribe in tribes.Where(tribe => tribe.Id > 0))
        {
            await session.ExecuteAsync(
                """
                INSERT INTO cards_tribes (card_id, tribe_id, is_primary)
                VALUES (@card_id, @tribe_id, @is_primary)
                ON CONFLICT DO NOTHING;
                """,
                [
                    new NpgsqlParameter("card_id", cardId),
                    new NpgsqlParameter("tribe_id", tribe.Id),
                    new NpgsqlParameter("is_primary", primary),
                ],
                cancellationToken);

            primary = false;
        }
    }

    private static async Task LoadTribesAsync(
        OrmSession session,
        IReadOnlyList<Card> cards,
        CancellationToken cancellationToken)
    {
        if (cards.Count == 0)
        {
            return;
        }

        var tribes = await session.QueryAsync<Tribe>(
            "SELECT * FROM tribes ORDER BY name;",
            cancellationToken: cancellationToken);

        var links = await session.QueryAsync<CardTribeLink>(
            "SELECT card_id, tribe_id, is_primary FROM cards_tribes;",
            cancellationToken: cancellationToken);

        var tribesById = tribes.ToDictionary(tribe => tribe.Id);
        var cardsById = cards.ToDictionary(card => card.Id);

        foreach (var link in links)
        {
            if (!cardsById.TryGetValue(link.CardId, out var card)
                || !tribesById.TryGetValue(link.TribeId, out var tribe))
            {
                continue;
            }

            card.Tribes.Add(tribe);
        }
    }
}
