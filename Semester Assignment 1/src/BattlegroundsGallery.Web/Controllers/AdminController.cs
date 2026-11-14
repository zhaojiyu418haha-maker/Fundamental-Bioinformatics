using BattlegroundsGallery.Data.Models;
using BattlegroundsGallery.Data.Repositories;
using BattlegroundsGallery.Mvc;

namespace BattlegroundsGallery.Web.Controllers;

public sealed class AdminController : ControllerBase
{
    private readonly ICardRepository _cards;
    private readonly ITribeRepository _tribes;

    public AdminController(ICardRepository cards, ITribeRepository tribes)
    {
        _cards = cards;
        _tribes = tribes;
    }

    public ViewResult Index(string? username = null)
    {
        return View("Admin/Index", new
        {
            Username = username ?? string.Empty,
        });
    }

    public async Task<ActionResult> Cards(CancellationToken cancellationToken = default)
    {
        var cards = await _cards.GetAllAsync(cancellationToken);

        return View("Admin/Cards", new
        {
            Title = "卡牌管理",
            Cards = cards,
        });
    }

    public async Task<ActionResult> Create(CancellationToken cancellationToken = default)
    {
        var card = new Card
        {
            Tier = 1,
            Attack = 1,
            Health = 1,
        };

        return View(
            "Admin/CardForm",
            await BuildFormModelAsync(card, "/admin/cards/create", "新增卡牌", cancellationToken));
    }

    public async Task<ActionResult> Edit(int id, CancellationToken cancellationToken = default)
    {
        var card = await _cards.GetByIdAsync(id, cancellationToken);

        if (card is null)
        {
            return Content("没有找到这张卡牌。");
        }

        return View(
            "Admin/CardForm",
            await BuildFormModelAsync(card, "/admin/cards/edit", "编辑卡牌", cancellationToken));
    }

    public async Task<ActionResult> Form(
        Card card,
        string action,
        string title,
        string? error = null,
        CancellationToken cancellationToken = default)
    {
        return View(
            "Admin/CardForm",
            await BuildFormModelAsync(
                card,
                action,
                title,
                cancellationToken,
                error));
    }

    private async Task<object> BuildFormModelAsync(
        Card card,
        string action,
        string title,
        CancellationToken cancellationToken,
        string? error = null)
    {
        var tribes = await _tribes.GetAllAsync(cancellationToken);
        var selectedIds = card.Tribes.Select(tribe => tribe.Id).ToHashSet();

        return new
        {
            Title = title,
            Action = action,
            Card = card,
            Tribes = tribes.Select(tribe => new
            {
                tribe.Id,
                tribe.Name,
                Selected = selectedIds.Contains(tribe.Id),
            }),
            Error = error ?? string.Empty,
        };
    }
}
