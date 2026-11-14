namespace BattlegroundsGallery.Web.Models;

public sealed class FeedbackViewModel
{
    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string Error { get; set; } = string.Empty;

    public bool Success { get; set; }
}
