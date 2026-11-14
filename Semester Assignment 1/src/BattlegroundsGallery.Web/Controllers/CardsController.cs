using BattlegroundsGallery.Data.Repositories;
using BattlegroundsGallery.Mvc;

namespace BattlegroundsGallery.Web.Controllers;

public sealed class CardsController : ControllerBase
{
    private readonly ICardRepository _cards;

    public CardsController(ICardRepository cards)
    {
        _cards = cards;
    }

    public async Task<ActionResult> Index(
        string? q,
        CancellationToken cancellationToken = default)
    {
        var cards = string.IsNullOrWhiteSpace(q)
            ? await _cards.GetAllAsync(cancellationToken)
            : await _cards.SearchAsync(q, cancellationToken);

        var items = cards.Select(card => new
        {
            card.Id,
            card.Name,
            card.Description,
            card.Attack,
            card.Health,
            card.Tier,
            card.ImagePath,
            TribeNames = string.Join(",", card.Tribes.Select(tribe => tribe.Name)),
        }).ToList();

        return View("Cards/Index", new
        {
            Title = "随从图鉴",
            Query = q ?? string.Empty,
            Count = items.Count,
            Cards = items,
        });
    }

    public async Task<ActionResult> Details(
        int id,
        CancellationToken cancellationToken = default)
    {
        var card = await _cards.GetByIdAsync(id, cancellationToken);
        return View("Cards/Details", card);
    }
}
