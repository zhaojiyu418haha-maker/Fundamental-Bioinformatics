using BattlegroundsGallery.Mvc;

namespace BattlegroundsGallery.Web.Controllers;

public sealed class AccountController : ControllerBase
{
    public ViewResult Login(string? error = null)
    {
        return View("Account/Login", new
        {
            Error = error ?? string.Empty,
        });
    }

    public ViewResult Register() => View("Account/Register");

    public ViewResult ForgotPassword() => View("Account/ForgotPassword");
}
