using BattlegroundsGallery.Web.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BattlegroundsGallery.UnitTests;

[TestClass]
public sealed class ValidationTests
{
    [TestMethod]
    public void FeedbackValidatorAcceptsValidData()
    {
        var model = new FeedbackViewModel
        {
            Name = "测试用户",
            Email = "test@example.com",
            Message = "这是一条有效的留言。",
        };

        Assert.IsNull(FeedbackValidator.Validate(model));
    }

    [TestMethod]
    public void FeedbackValidatorRejectsInvalidEmailAndShortMessage()
    {
        var model = new FeedbackViewModel
        {
            Name = "A",
            Email = "not-an-email",
            Message = "短",
        };

        var error = FeedbackValidator.Validate(model);

        Assert.IsNotNull(error);
        StringAssert.Contains(error, "姓名");
        StringAssert.Contains(error, "邮箱");
        StringAssert.Contains(error, "留言");
    }
}
