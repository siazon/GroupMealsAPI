using App.Domain.Common.Setting;
using GenFu;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace App.UnitTest.Domain
{
    [TestClass]
    public class WsSettingTest
    {
        [TestMethod]
        public void Create()
        {
            var item = A.New<DbSetting>();

            var json = JsonConvert.SerializeObject(item);

            Assert.IsNotNull(json);
        }
    }
}