namespace BattlegroundsGallery.Templating;

/// <summary>
/// Contract for the custom Razor-like template engine.
/// Final version must support variables, loops and conditions.
/// </summary>
public interface ITemplateEngine
{
    string Render(string template, object? model = null);
}
