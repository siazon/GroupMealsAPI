using App.Domain.Config;
using App.Infrastructure.Extensions;
using App.Infrastructure.Utility.Common;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;

namespace App.UnitTest.Infrastructure.Utility
{
    [TestClass]
    public class DateTimeUtilTest
    {
        [TestMethod]
        public void CovertDaysOfWeekTime()
        {
            var input = new DateTime(2020, 8, 13, 22, 48, 0);
            var output = new DateTime(2000, 1, 4, 22, 48, 0);

            var mock = new Mock<IOptions<AppSettingConfig>>();

            var result = new DateTimeUtil(mock.Object).CovertDaysOfWeekTime(input, input.GetFoodToday().DayOfWeek.ToString());
            Assert.IsTrue(result.Value.Ticks == output.Ticks);
        }

        [TestMethod]
        public void CovertDaysOfWeekTimeEarlyMorning()
        {
            var input = new DateTime(2020, 8, 14, 2, 0, 0);
            var output = new DateTime(2000, 1, 5, 2, 0, 0);
            var inputDaysOfWeek = input.GetFoodToday().DayOfWeek;

            var mock = new Mock<IOptions<AppSettingConfig>>();

            var result = new DateTimeUtil(mock.Object).CovertDaysOfWeekTime(input, inputDaysOfWeek.ToString());

            Assert.IsTrue(inputDaysOfWeek == DayOfWeek.Thursday);
            Assert.IsTrue(result.Value.Ticks == output.Ticks);
        }
    }
}