using BattlegroundsGallery.Templating;

namespace BattlegroundsGallery.Web.Infrastructure;

public sealed class ViewRenderer
{
    private readonly string _viewsRoot;
    private readonly string _viewsRootWithSeparator;
    private readonly ITemplateEngine _templateEngine;

    public ViewRenderer(string viewsRoot, ITemplateEngine? templateEngine = null)
    {
        if (string.IsNullOrWhiteSpace(viewsRoot))
        {
            throw new ArgumentException("Views root is required.", nameof(viewsRoot));
        }

        _viewsRoot = Path.GetFullPath(viewsRoot);
        _viewsRootWithSeparator = _viewsRoot.TrimEnd(
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

        _templateEngine = templateEngine ?? new TemplateEngine();
    }

    public async Task<string> RenderAsync(string viewName, object? model)
    {
        var relativePath = viewName.Replace('/', Path.DirectorySeparatorChar);

        if (!relativePath.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
        {
            relativePath += ".html";
        }

        var fullPath = Path.GetFullPath(Path.Combine(_viewsRoot, relativePath));

        if (!fullPath.StartsWith(_viewsRootWithSeparator, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("View path is outside the Views directory.");
        }

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"View not found: {viewName}", fullPath);
        }

        var template = await File.ReadAllTextAsync(fullPath);
        return _templateEngine.Render(template, model);
    }
}