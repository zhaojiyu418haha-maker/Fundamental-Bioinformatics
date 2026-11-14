using BattlegroundsGallery.Server.Http;
using BattlegroundsGallery.Server.Routing;
using BattlegroundsGallery.Server.StaticFiles;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BattlegroundsGallery.UnitTests;

[TestClass]
public sealed class ServerTests
{
    [TestMethod]
    public void RouterSeparatesGetAndPostRoutes()
    {
        var router = new Router();

        router.MapGet("/cards", _ => Task.CompletedTask);
        router.MapPost("/cards", _ => Task.CompletedTask);

        Assert.IsTrue(router.TryGetHandler("GET", "/cards", out _));
        Assert.IsTrue(router.TryGetHandler("POST", "/cards", out _));
        Assert.IsFalse(router.TryGetHandler("GET", "/missing", out _));
    }

    [TestMethod]
    public void RouterNormalizesTrailingSlash()
    {
        var router = new Router();
        router.MapGet("/cards/", _ => Task.CompletedTask);

        Assert.IsTrue(router.TryGetHandler("GET", "/cards", out _));
        Assert.IsTrue(router.TryGetHandler("GET", "/cards/", out _));
    }

    [TestMethod]
    public void NormalizePathHandlesRootAndTrailingSlash()
    {
        Assert.AreEqual("/", RequestContext.NormalizePath(null));
        Assert.AreEqual("/", RequestContext.NormalizePath(""));
        Assert.AreEqual("/cards", RequestContext.NormalizePath("/cards/"));
    }

    [TestMethod]
    public void MimeTypesReturnExpectedTypes()
    {
        Assert.AreEqual("text/html; charset=utf-8", MimeTypes.GetContentType("index.html"));
        Assert.AreEqual("text/css; charset=utf-8", MimeTypes.GetContentType("style.css"));
        Assert.AreEqual("image/png", MimeTypes.GetContentType("card.png"));
        Assert.AreEqual("application/octet-stream", MimeTypes.GetContentType("unknown.xyz"));
    }
}
