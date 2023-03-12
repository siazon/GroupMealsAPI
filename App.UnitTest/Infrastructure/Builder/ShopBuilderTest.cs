using App.Infrastructure.Builders.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace App.UnitTest.Infrastructure.Builder
{
    [TestClass]
    public class ShopBuilderTest
    {
        [TestMethod]
        public void Create()
        {
            var builder = new ShopBuilder();
            var item = builder.GeneralShopInfo().Build();
            var json = JsonConvert.SerializeObject(item);

            Assert.IsNotNull(item);
        }
    }
}