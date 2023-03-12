using App.Domain.Common.Content;
using GenFu;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;

namespace App.UnitTest.Domain
{
    [TestClass]
    public class WsShopContentTest
    {
        [TestMethod]
        public void Create()
        {
            var item = A.New<DbShopContent>();
            item.Content = Resource1.emailtemplatebooking;

            var json = JsonConvert.SerializeObject(item);

            Assert.IsNotNull(json);
            Console.WriteLine(json);
        }
    }
}