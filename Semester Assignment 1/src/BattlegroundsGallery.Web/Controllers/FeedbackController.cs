using BattlegroundsGallery.Mvc;
using BattlegroundsGallery.Web.Models;

namespace BattlegroundsGallery.Web.Controllers;

public sealed class FeedbackController : ControllerBase
{
    public ViewResult Index(FeedbackViewModel? model = null)
        => View("Feedback/Index", model ?? new FeedbackViewModel());
}
