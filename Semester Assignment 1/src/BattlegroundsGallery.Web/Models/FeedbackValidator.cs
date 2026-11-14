using System.Text.RegularExpressions;

namespace BattlegroundsGallery.Web.Models;

public static partial class FeedbackValidator
{
    public static string? Validate(FeedbackViewModel model)
    {
        var errors = new List<string>();

        if (!NamePattern().IsMatch(model.Name))
        {
            errors.Add("姓名需要是 2–64 个字符。");
        }

        if (!EmailPattern().IsMatch(model.Email))
        {
            errors.Add("请输入有效的邮箱地址。");
        }

        if (model.Message.Length is < 5 or > 1000)
        {
            errors.Add("留言长度需要在 5–1000 个字符之间。");
        }

        return errors.Count == 0 ? null : string.Join(" ", errors);
    }

    [GeneratedRegex(@"^[\p{L}\p{N}·'’ .\-]{2,64}$", RegexOptions.Compiled)]
    private static partial Regex NamePattern();

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex EmailPattern();
}
