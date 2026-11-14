namespace BattlegroundsGallery.Mvc;

/// <summary>
/// Writes a plain response without a view.
/// </summary>
public sealed class ContentResult : ActionResult
{
    public ContentResult(string content, string contentType = "text/plain; charset=utf-8")
    {
        Content = content;
        ContentType = contentType;
    }

    public string Content { get; }

    public string ContentType { get; }
}
