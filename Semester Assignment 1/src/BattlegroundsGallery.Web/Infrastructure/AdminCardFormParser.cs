using System.Globalization;
using System.Text.RegularExpressions;
using BattlegroundsGallery.Data.Models;

namespace BattlegroundsGallery.Web.Infrastructure;

public sealed record AdminCardInput(
    Card Card,
    IReadOnlyList<int> TribeIds,
    string? Error);

public static partial class AdminCardFormParser
{
    public static AdminCardInput Parse(
        IReadOnlyDictionary<string, IReadOnlyList<string>> form,
        int? id)
    {
        var card = new Card
        {
            Id = id ?? 0,
            Slug = Get(form, "slug").Trim().ToLowerInvariant(),
            Name = Get(form, "name").Trim(),
            Description = Get(form, "description").Trim(),
            ImagePath = Get(form, "image_path").Trim(),
        };

        var errors = new List<string>();

        if (!SlugPattern().IsMatch(card.Slug))
        {
            errors.Add("slug 只能包含小写字母、数字和连字符，长度 1–120。");
        }

        if (!NamePattern().IsMatch(card.Name))
        {
            errors.Add("卡牌名称格式不正确，长度应为 1–100 个字符。");
        }

        if (card.Description.Length is < 1 or > 2000)
        {
            errors.Add("描述长度应为 1–2000 个字符。");
        }

        if (!TryReadNumber(form, "attack", 0, 100, out var attack))
        {
            errors.Add("攻击力必须是 0–100 的整数。");
        }

        if (!TryReadNumber(form, "health", 0, 100, out var health))
        {
            errors.Add("生命值必须是 0–100 的整数。");
        }

        if (!TryReadNumber(form, "tier", 1, 7, out var tier))
        {
            errors.Add("酒馆等级必须是 1–7 的整数。");
        }

        if (!ImagePathPattern().IsMatch(card.ImagePath))
        {
            errors.Add("图片路径格式应为 assets/cards/文件名.png。");
        }

        var tribeIds = GetValues(form, "tribe_ids")
            .Select(value => int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var tribeId)
                ? tribeId
                : 0)
            .Where(tribeId => tribeId > 0)
            .Distinct()
            .ToArray();

        if (tribeIds.Length == 0)
        {
            errors.Add("请至少选择一个种族。");
        }

        card.Attack = attack;
        card.Health = health;
        card.Tier = tier;

        return new AdminCardInput(
            card,
            tribeIds,
            errors.Count == 0 ? null : string.Join(" ", errors));
    }

    private static bool TryReadNumber(
        IReadOnlyDictionary<string, IReadOnlyList<string>> form,
        string key,
        int min,
        int max,
        out int value)
        => int.TryParse(
               Get(form, key),
               NumberStyles.Integer,
               CultureInfo.InvariantCulture,
               out value)
           && value >= min
           && value <= max;

    private static string Get(
        IReadOnlyDictionary<string, IReadOnlyList<string>> form,
        string key)
        => form.TryGetValue(key, out var values) && values.Count > 0
            ? values[^1]
            : string.Empty;

    private static IEnumerable<string> GetValues(
        IReadOnlyDictionary<string, IReadOnlyList<string>> form,
        string key)
        => form.TryGetValue(key, out var values) ? values : [];

    [GeneratedRegex(@"^[\p{L}\p{N}·\-'’ ]{1,100}$", RegexOptions.Compiled)]
    private static partial Regex NamePattern();

    [GeneratedRegex(@"^[a-z0-9][a-z0-9\-]{0,119}$", RegexOptions.Compiled)]
    private static partial Regex SlugPattern();

    [GeneratedRegex(@"^assets/cards/[a-z0-9\-]+\.png$", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex ImagePathPattern();
}
