using BattlegroundsGallery.Mvc;
using BattlegroundsGallery.Server.Http;

namespace BattlegroundsGallery.Web.Infrastructure;

public static class MvcExecutor
{
    public static async Task ExecuteAsync<TController>(
        RequestContext context,
        TController controller,
        Func<TController, ActionResult> action,
        ViewRenderer views)
        where TController : ControllerBase
    {
        var result = action(controller);
        await WriteResultAsync(context, result, views);
    }

    public static async Task ExecuteAsync<TController>(
        RequestContext context,
        TController controller,
        Func<TController, Task<ActionResult>> action,
        ViewRenderer views)
        where TController : ControllerBase
    {
        var result = await action(controller);
        await WriteResultAsync(context, result, views);
    }

    private static async Task WriteResultAsync(
        RequestContext context,
        ActionResult result,
        ViewRenderer views)
    {
        switch (result)
        {
            case ViewResult view:
            {
                var html = await views.RenderAsync(view.ViewName, view.Model);
                await context.WriteHtmlAsync(html);
                break;
            }

            case ContentResult content:
            {
                await context.WriteTextAsync(content.Content, content.ContentType);
                break;
            }

            default:
            {
                throw new InvalidOperationException(
                    $"Unsupported action result: {result.GetType().Name}");
            }
        }
    }
}
