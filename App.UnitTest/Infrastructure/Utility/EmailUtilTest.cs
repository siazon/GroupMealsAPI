using App.Domain.Common.Setting;
using App.Domain.Enum;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using App.Infrastructure.Utility.Common;

namespace App.UnitTest.Infrastructure.Utility
{
#if !DEBUG
[Ignore]
#endif

    [TestClass]
    public class EmailUtilTest
    {
        [TestMethod]
        public void Test()
        {
            var emailutil = new EmailUtil();
            var settings = new List<DbSetting>();
            settings.Add(new DbSetting() { SettingKey = ServerSettingEnum.AppSmtpenabled, SettingValue = "true", });
            settings.Add(new DbSetting() { SettingKey = ServerSettingEnum.AppEmailApiKey, SettingValue = "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", });

            var result = emailutil.SendEmail(settings, "bingliangchan@hotmail.com", "sample from", "bingliangchan@gmail.com", "sample to",
                "test subject", "", "test content", null,null).Result;

            Assert.IsTrue(result);
        }
    }
}