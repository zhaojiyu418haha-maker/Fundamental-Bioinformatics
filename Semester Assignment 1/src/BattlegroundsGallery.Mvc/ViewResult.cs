namespace BattlegroundsGallery.Mvc;

/// <summary>
/// Describes a view that should be rendered with the custom template engine.
/// </summary>
public sealed class ViewResult : ActionResult
{
    public ViewResult(string viewName, object? model = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(viewName);
        ViewName = viewName;
        Model = model;
    }

    public string ViewName { get; }

    public object? Model { get; }
}
