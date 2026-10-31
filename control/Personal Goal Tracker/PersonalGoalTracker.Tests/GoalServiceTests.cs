using Microsoft.VisualStudio.TestTools.UnitTesting;
using PersonalGoalTracker.Core;

namespace PersonalGoalTracker.Tests
{
    [TestClass]
    public class GoalServiceTests
    {
        [TestMethod]
        public void Create_ValidData_ReturnsTrue_And_DefaultStatusActive()
        {
            // Arrange (准备)
            var service = new GoalService();

            // Act (执行)
            bool result = service.Create("Read a book", "week", out string error);

            // Assert (断言)
            Assert.IsTrue(result);
            Assert.IsNull(error);

            var list = service.GetAll(null);
            Assert.AreEqual(1, list.Count);
            Assert.AreEqual("active", list[0].Status); // 测试默认状态
        }

        [TestMethod]
        public void Create_ShortTitle_ReturnsFalse()
        {
            var service = new GoalService();
            bool result = service.Create("abc", "week", out string error); // 长度为3，不合规

            Assert.IsFalse(result);
            Assert.IsNotNull(error);
        }
    }
}