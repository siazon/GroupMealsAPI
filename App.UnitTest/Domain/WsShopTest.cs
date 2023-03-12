using App.Domain.Common.Shop;
using GenFu;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;

namespace App.UnitTest.Domain
{
    [TestClass]
    public class WsShopTest
    {
        [TestMethod]
        public void Create()
        {
            var item = A.New<DbShop>();

            item.ShopName = "Demo IRE Shop";
            item.ShopNumber = "(0)12896242";
            item.ShopWeChat = "(0)12896242";
            item.ShopWeChatQRCode = "(0)12896242";
            item.ShopNumber2 = "(01)5344336";
            item.ShopMobile = "0871111111";
            item.Website = "http://eternal.kingfood.ie";

            var json = JsonConvert.SerializeObject(item);

            Assert.IsNotNull(json);
            Console.WriteLine(json);
        }
    }
}