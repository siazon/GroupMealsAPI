using App.Infrastructure.Utility.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace App.UnitTest.Infrastructure.Utility
{
    [TestClass]
    public class EncryptionHelperTest
    {
        [TestMethod]
        public void EncodeAndDecode()
        {
            var encryptionHelper = new EncryptionHelper();
            var password = "1234";

            var encode = encryptionHelper.EncryptString(password);
            var decode = encryptionHelper.DecryptString(encode);

            Console.WriteLine(encode);
            Assert.IsNotNull(encode);
            Assert.AreEqual(decode, password);
        }
    }
}