using BattlegroundsGallery.Mvc;

namespace BattlegroundsGallery.Web.Controllers;

public sealed class HomeController : ControllerBase
{
    public ViewResult Index()
    {
        return View("Home/Index", new
        {
            Title = "酒馆战棋随从图鉴",
            Message = "这个页面已经由 C# MVC 路由和模板引擎生成。",
        });
    }
}