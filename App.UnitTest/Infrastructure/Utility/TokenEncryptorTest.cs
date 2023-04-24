using App.Domain.Common.Auth;
using App.Infrastructure.Utility.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace App.UnitTest.Infrastructure.Utility
{
    [TestClass]
    public class TokenEncryptorTest
    {
        [TestMethod]
        public void EncodeAndDecode()
        {
            var tokenEn = new TokenEncryptorHelper();

            /*
             *  Server Key = ShopInfo.TokenKey client app
             *  Server Key = ShopInfo.AdminTokenKey  Admin App
             *  Server Key = 7349C359-899C-4CCB-ABF7-65C927FF1112   Master Key
             *  Shop Id must match except Master Key(shopId can =0)
             */
            var tokenObj = new DbToken()
            {
                ShopId = 13,
                ExpiredTime = DateTime.UtcNow.AddYears(100),
                //ServerKey = Guid.NewGuid().ToString(),
                ServerKey = "41A2AEFF-B784-4CAF-A00F-C72C1D0CADF4",
                ShopKey = "49C20A"
            };

            var encodestring = tokenEn.Encrypt(tokenObj);
            Console.WriteLine("encodestring:::" + encodestring);
            Assert.IsNotNull(encodestring);

            var decodeobj = tokenEn.Decrypt<DbToken>(encodestring);

            Assert.AreEqual(tokenObj.ShopId, decodeobj.ShopId);
            Assert.AreEqual(tokenObj.ExpiredTime, decodeobj.ExpiredTime);
            Assert.AreEqual(tokenObj.ServerKey, decodeobj.ServerKey);
        }
    }
}