using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using BattlegroundsGallery.Data;
using BattlegroundsGallery.Data.Models;
using BattlegroundsGallery.Data.Repositories;
using BattlegroundsGallery.Data.Security;
using BattlegroundsGallery.Server;
using BattlegroundsGallery.Server.Http;
using BattlegroundsGallery.Server.Routing;
using BattlegroundsGallery.Server.StaticFiles;
using BattlegroundsGallery.Web.Controllers;
using BattlegroundsGallery.Web.Infrastructure;
using BattlegroundsGallery.Web.Models;

var options = HttpServerOptions.Parse(args);

Console.WriteLine(
    $"[startup] Runtime={RuntimeInformation.FrameworkDescription} " +
    $"Arch={RuntimeInformation.ProcessArchitecture} " +
    $"PID={Environment.ProcessId} " +
    $"DebuggerAttached={Debugger.IsAttached} " +
    $"HttpListenerSupported={HttpListener.IsSupported}");

if (!Directory.Exists(options.WebRootPath))
{
    Console.Error.WriteLine($"Web root was not found: {options.WebRootPath}");
    Console.Error.WriteLine("Build the project first, or pass --webroot <path>.");
    return 1;
}

var router = new Router();
router.MapGet("/health", context => context.WriteJsonAsync(new
{
    status = "ok",
    service = "BattlegroundsGallery.Web",
    framework = ".NET 9 / HttpListener",
    timestampUtc = DateTime.UtcNow,
}));

router.MapGet("/api/status", context => context.WriteJsonAsync(new
{
    project = "BattlegroundsGallery",
    stage = "server-skeleton",
    database = "not configured yet",
    cards = "static wwwroot assets",
}));
var views = new ViewRenderer(
    Path.Combine(AppContext.BaseDirectory, "Views"));

var cardRepository = new CardRepository(
    DatabaseOptions.GetConnectionString());

var userRepository = new UserRepository(
    DatabaseOptions.GetConnectionString());

var tribeRepository = new TribeRepository(
    DatabaseOptions.GetConnectionString());

var feedbackRepository = new FeedbackRepository(
    DatabaseOptions.GetConnectionString());

var sessions = new SessionStore();

SessionInfo? GetAdminSession(RequestContext context)
{
    var session = sessions.Get(context.GetCookie(SessionStore.CookieName));
    return session?.Role == "admin" ? session : null;
}

string GetFormValue(
    IReadOnlyDictionary<string, IReadOnlyList<string>> form,
    string key)
    => form.TryGetValue(key, out var values) && values.Count > 0
        ? values[^1]
        : string.Empty;

router.MapGet("/home", context =>
    MvcExecutor.ExecuteAsync(
        context,
        new HomeController(),
        controller => controller.Index(),
        views));

router.MapGet("/", context => context.RedirectAsync("/cards"));

router.MapGet("/cards", context =>
{
    var query = context.Query.TryGetValue("q", out var value) ? value : null;

    return MvcExecutor.ExecuteAsync(
        context,
        new CardsController(cardRepository),
        controller => controller.Index(query),
        views);
});

router.MapGet("/cards/details", context =>
{
    var id = context.Query.TryGetValue("id", out var rawId)
             && int.TryParse(rawId, out var parsedId)
        ? parsedId
        : 0;

    return MvcExecutor.ExecuteAsync(
        context,
        new CardsController(cardRepository),
        controller => controller.Details(id),
        views);
});

router.MapGet("/api/cards/search", async context =>
{
    var query = context.Query.TryGetValue("q", out var value) ? value : string.Empty;
    var cards = await cardRepository.SearchAsync(query);

    await context.WriteJsonAsync(cards.Select(card => new
    {
        card.Id,
        card.Name,
        card.Description,
        card.Attack,
        card.Health,
        card.Tier,
        card.ImagePath,
        Tribes = card.Tribes.Select(tribe => tribe.Name).ToArray(),
    }));
});

router.MapGet("/account/login", context =>
    MvcExecutor.ExecuteAsync(
        context,
        new AccountController(),
        controller => controller.Login(),
        views));

router.MapGet("/feedback", context =>
    MvcExecutor.ExecuteAsync(
        context,
        new FeedbackController(),
        controller => controller.Index(),
        views));

router.MapPost("/feedback", async context =>
{
    var form = await context.ReadFormAsync();
    var model = new FeedbackViewModel
    {
        Name = (form.TryGetValue("name", out var name) ? name : string.Empty).Trim(),
        Email = (form.TryGetValue("email", out var email) ? email : string.Empty).Trim(),
        Message = (form.TryGetValue("message", out var message) ? message : string.Empty).Trim(),
    };

    var error = FeedbackValidator.Validate(model);
    if (error is not null)
    {
        model.Error = error;

        await MvcExecutor.ExecuteAsync(
            context,
            new FeedbackController(),
            controller => controller.Index(model),
            views);
        return;
    }

    await feedbackRepository.CreateAsync(new Feedback
    {
        Name = model.Name,
        Email = model.Email,
        Message = model.Message,
    });

    await MvcExecutor.ExecuteAsync(
        context,
        new FeedbackController(),
        controller => controller.Index(new FeedbackViewModel { Success = true }),
        views);
});

router.MapPost("/account/login", async context =>
{
    var form = await context.ReadFormAsync();
    var username = form.TryGetValue("username", out var rawUsername)
        ? rawUsername.Trim()
        : string.Empty;
    var password = form.TryGetValue("password", out var rawPassword)
        ? rawPassword
        : string.Empty;

    var user = await userRepository.FindByUsernameAsync(username);

    if (user is null
        || user.Role != "admin"
        || !PasswordHasher.Verify(password, user.PasswordHash))
    {
        await MvcExecutor.ExecuteAsync(
            context,
            new AccountController(),
            controller => controller.Login("用户名、密码或管理员权限不正确。"),
            views);
        return;
    }

    var sessionId = sessions.Create(user);
    context.SetCookie(
        SessionStore.CookieName,
        sessionId,
        SessionStore.GetCookieExpiration());

    await context.RedirectAsync("/admin");
});

router.MapGet("/account/me", async context =>
{
    var session = sessions.Get(context.GetCookie(SessionStore.CookieName));

    if (session is null)
    {
        await context.WriteJsonAsync(new { authenticated = false }, 401);
        return;
    }

    await context.WriteJsonAsync(new
    {
        authenticated = true,
        session.UserId,
        session.Username,
        session.Role,
    });
});

router.MapPost("/account/logout", async context =>
{
    sessions.Remove(context.GetCookie(SessionStore.CookieName));
    context.DeleteCookie(SessionStore.CookieName);
    await context.RedirectAsync("/account/login");
});

router.MapGet("/admin", async context =>
{
    var session = GetAdminSession(context);

    if (session is null)
    {
        await context.RedirectAsync("/account/login");
        return;
    }

    await MvcExecutor.ExecuteAsync(
        context,
        new AdminController(cardRepository, tribeRepository),
        controller => controller.Index(session.Username),
        views);
});

router.MapGet("/admin/cards", async context =>
{
    if (GetAdminSession(context) is null)
    {
        await context.RedirectAsync("/account/login");
        return;
    }

    await MvcExecutor.ExecuteAsync(
        context,
        new AdminController(cardRepository, tribeRepository),
        controller => controller.Cards(),
        views);
});

router.MapGet("/admin/cards/create", async context =>
{
    if (GetAdminSession(context) is null)
    {
        await context.RedirectAsync("/account/login");
        return;
    }

    await MvcExecutor.ExecuteAsync(
        context,
        new AdminController(cardRepository, tribeRepository),
        controller => controller.Create(),
        views);
});

router.MapGet("/admin/cards/edit", async context =>
{
    if (GetAdminSession(context) is null)
    {
        await context.RedirectAsync("/account/login");
        return;
    }

    var id = context.Query.TryGetValue("id", out var rawId)
             && int.TryParse(rawId, out var parsedId)
        ? parsedId
        : 0;

    await MvcExecutor.ExecuteAsync(
        context,
        new AdminController(cardRepository, tribeRepository),
        controller => controller.Edit(id),
        views);
});

router.MapPost("/admin/cards/create", async context =>
{
    if (GetAdminSession(context) is null)
    {
        await context.RedirectAsync("/account/login");
        return;
    }

    var form = await context.ReadFormValuesAsync();
    var input = AdminCardFormParser.Parse(form, id: null);

    if (input.Error is null
        && await cardRepository.SlugExistsAsync(input.Card.Slug))
    {
        input = input with { Error = "这个 slug 已经被其他卡牌使用。" };
    }

    if (input.Error is not null)
    {
        input.Card.Tribes = input.TribeIds
            .Select(tribeId => new Tribe { Id = tribeId })
            .ToList();

        await MvcExecutor.ExecuteAsync(
            context,
            new AdminController(cardRepository, tribeRepository),
            controller => controller.Form(
                input.Card,
                "/admin/cards/create",
                "新增卡牌",
                input.Error),
            views);
        return;
    }

    input.Card.Tribes = input.TribeIds
        .Select(tribeId => new Tribe { Id = tribeId })
        .ToList();

    await cardRepository.CreateAsync(input.Card);
    await context.RedirectAsync("/admin/cards");
});

router.MapPost("/admin/cards/edit", async context =>
{
    if (GetAdminSession(context) is null)
    {
        await context.RedirectAsync("/account/login");
        return;
    }

    var form = await context.ReadFormValuesAsync();
    var id = int.TryParse(GetFormValue(form, "id"), out var parsedId)
        ? parsedId
        : 0;

    var input = AdminCardFormParser.Parse(form, id);

    if (input.Error is null
        && await cardRepository.SlugExistsAsync(input.Card.Slug, id))
    {
        input = input with { Error = "这个 slug 已经被其他卡牌使用。" };
    }

    if (input.Error is not null)
    {
        input.Card.Tribes = input.TribeIds
            .Select(tribeId => new Tribe { Id = tribeId })
            .ToList();

        await MvcExecutor.ExecuteAsync(
            context,
            new AdminController(cardRepository, tribeRepository),
            controller => controller.Form(
                input.Card,
                "/admin/cards/edit",
                "编辑卡牌",
                input.Error),
            views);
        return;
    }

    var existing = await cardRepository.GetByIdAsync(id);
    if (existing is null)
    {
        await context.RedirectAsync("/admin/cards");
        return;
    }

    existing.Slug = input.Card.Slug;
    existing.Name = input.Card.Name;
    existing.Description = input.Card.Description;
    existing.Attack = input.Card.Attack;
    existing.Health = input.Card.Health;
    existing.Tier = input.Card.Tier;
    existing.ImagePath = input.Card.ImagePath;
    existing.Tribes = input.TribeIds
        .Select(tribeId => new Tribe { Id = tribeId })
        .ToList();

    await cardRepository.UpdateAsync(existing);
    await context.RedirectAsync("/admin/cards");
});

router.MapPost("/admin/cards/delete", async context =>
{
    if (GetAdminSession(context) is null)
    {
        await context.RedirectAsync("/account/login");
        return;
    }

    var form = await context.ReadFormValuesAsync();
    var id = int.TryParse(GetFormValue(form, "id"), out var parsedId)
        ? parsedId
        : 0;

    if (id > 0)
    {
        await cardRepository.DeleteAsync(id);
    }

    await context.RedirectAsync("/admin/cards");
});

using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellation.Cancel();
};

try
{
    if (HttpListener.IsSupported)
    {
        await using var server = new HttpServer(
            options,
            router,
            new StaticFileMiddleware(options.WebRootPath),
            message => Console.WriteLine(message));

        await server.RunAsync(cancellation.Token);
    }
    else
    {
        Console.WriteLine(
            "HttpListener.IsSupported = false on this Windows installation. " +
            "Starting the TcpListener compatibility server instead.");

        await using var server = new TcpHttpServer(options, message => Console.WriteLine(message));
        await server.RunAsync(cancellation.Token);
    }

    return 0;
}
catch (InvalidOperationException exception)
{
    Console.Error.WriteLine(exception.Message);
    return 2;
}
