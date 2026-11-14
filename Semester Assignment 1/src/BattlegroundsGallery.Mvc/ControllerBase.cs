namespace BattlegroundsGallery.Mvc;

/// <summary>
/// Minimal base controller. Routing and view rendering are intentionally
/// implemented by our own framework rather than ASP.NET Core MVC.
/// </summary>
public abstract class ControllerBase
{
    protected ViewResult View(string viewName, object? model = null)
        => new(viewName, model);

    protected ContentResult Content(string content, string contentType = "text/plain; charset=utf-8")
        => new(content, contentType);
}
