using BattlegroundsGallery.Templating;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BattlegroundsGallery.UnitTests;

[TestClass]
public sealed class TemplateEngineTests
{
    private readonly TemplateEngine _engine = new();

    [TestMethod]
    public void RendersVariablesAndEncodesHtml()
    {
        var html = _engine.Render("<h1>{{ model.Title }}</h1>", new { Title = "鱼人 <卡牌>" });

        Assert.AreEqual("<h1>鱼人 &lt;卡牌&gt;</h1>", html);
    }

    [TestMethod]
    public void RendersRawHtmlWithTripleBraces()
    {
        var html = _engine.Render("<div>{{{ model.Html }}}</div>", new
        {
            Html = "<strong>RAW</strong>",
        });

        Assert.AreEqual("<div><strong>RAW</strong></div>", html);
    }

    [TestMethod]
    public void RendersIfAndElseBranches()
    {
        var template = """
            @if (model.Show)
            <p>SHOW</p>
            @else
            <p>HIDE</p>
            @endif
            """;

        var shown = _engine.Render(template, new { Show = true });
        var hidden = _engine.Render(template, new { Show = false });

        StringAssert.Contains(shown, "SHOW");
        Assert.IsFalse(shown.Contains("HIDE"));
        StringAssert.Contains(hidden, "HIDE");
        Assert.IsFalse(hidden.Contains("SHOW"));
    }

    [TestMethod]
    public void RendersForeachLoop()
    {
        var html = _engine.Render(
            "@foreach (var card in model.Cards)<li>{{ card.Name }}</li>@endforeach",
            new
            {
                Cards = new[]
                {
                    new { Name = "气泡枪手" },
                    new { Name = "塔德" },
                },
            });

        Assert.AreEqual("<li>气泡枪手</li><li>塔德</li>", html);
    }

    [TestMethod]
    public void RendersNestedLoopsAndConditions()
    {
        var template = """
            @foreach (var group in model.Groups)
            <h2>{{ group.Name }}</h2>
            @if (group.Items.Length > 0)
            @foreach (var item in group.Items)
            <span>{{ item }}</span>
            @endforeach
            @endif
            @endforeach
            """;

        var html = _engine.Render(template, new
        {
            Groups = new[]
            {
                new { Name = "鱼人", Items = new[] { "气泡枪手", "塔德" } },
                new { Name = "空组", Items = Array.Empty<string>() },
            },
        });

        StringAssert.Contains(html, "<h2>鱼人</h2>");
        StringAssert.Contains(html, "<span>气泡枪手</span>");
        StringAssert.Contains(html, "<h2>空组</h2>");
        Assert.IsFalse(html.Contains("<span></span>"));
    }

    [TestMethod]
    public void SupportsComparisonNegationAndStrings()
    {
        var template = """
            @if (model.Count >= 2)
            <p>OK</p>
            @endif
            @if (!model.Disabled)
            <p>ENABLED</p>
            @endif
            @if (model.Name == "鱼人")
            <p>FISH</p>
            @endif
            """;

        var html = _engine.Render(template, new
        {
            Count = 2,
            Disabled = false,
            Name = "鱼人",
        });

        StringAssert.Contains(html, "OK");
        StringAssert.Contains(html, "ENABLED");
        StringAssert.Contains(html, "FISH");
    }
}
