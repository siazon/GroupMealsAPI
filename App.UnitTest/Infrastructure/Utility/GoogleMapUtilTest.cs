using App.Infrastructure.Utility.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace App.UnitTest.Infrastructure.Utility
{
#if !DEBUG
[Ignore]
#endif

    [TestClass]
    public class GoogleMapUtilTest
    {
        [TestMethod]
        public void SuggestAddress()
        {
            var googleapi = new GoogleMapUtil();

            var result = googleapi.SuggestAddress("78 hunterswalk Dublin Ireland", "ie").Result;

            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void SearchByAddressByEircode()
        {
            var googleapi = new GoogleMapUtil();

            var result = googleapi.SuggestAddress("sw155lf", "gb").Result;

            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void SearchByAddressUK()
        {
            var googleapi = new GoogleMapUtil();

            var result = googleapi.SuggestAddress("Wellington Road, Hampton", "gb").Result;

            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void SearchByAddressUKPostCode()
        {
            var googleapi = new GoogleMapUtil();

            var result = googleapi.SuggestAddress("TW12 1JT", "gb").Result;

            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void SearchByAddressGeoCode()
        {
            var googleapi = new GoogleMapUtil();

            var result = googleapi.GetGeoAddress("78 Hunters Walk, Hunters Wood, Dublin 24, Ireland", "ie").Result;

            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void SearchByAddressOldGeoCode()
        {
            var googleapi = new GoogleMapUtil();

            var result = googleapi.GetGeoAddress("118 king's road dafdsfasdf", "gb").Result;

            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void SearchByAddressPostCodeGeoCode()
        {
            var googleapi = new GoogleMapUtil();

            var result = googleapi.GetGeoAddress("W128EU", "gb").Result;

            Assert.IsNull(result);
        }

        [TestMethod]
        public void SearchByAddressGeoCodeEircode()
        {
            var googleapi = new GoogleMapUtil();

            var result = googleapi.GetGeoAddress("Arabella Drive, London SW15 5LF, UK", "gb").Result;

            Assert.IsNull(result);
        }

        [TestMethod]
        public void SearchByAddressGeoCodeArea()
        {
            var googleapi = new GoogleMapUtil();

            var result = googleapi.GetGeoAddress("London, UK", "gb").Result;

            Assert.IsNull(result);
        }

        [TestMethod]
        public void SearchByPostCodeUK()
        {
            var googleapi = new GoogleMapUtil();

            var result = googleapi.GetGeoAddressByPostCode("TW12 1JT", "gb").Result;

            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void CalculateDistance()
        {
            var googleapi = new GoogleMapUtil();

            var result = googleapi.GetDistance("78 Hunters Walk, Hunters Wood, Dublin 24, Ireland", "16 beechwood court, Ireland").Result;

            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void CalculateDistanceByCode()
        {
            var googleapi = new GoogleMapUtil();

            var result = googleapi.GetDistance("TW12 1JT", "TW12 2TN").Result;

            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void CalculateDistanceMatrix()
        {
            var googleapi = new GoogleMapUtil();

            var result = googleapi.CalculateDistanceMatrix("53.2712187,-6.3292991", "53.276698,-6.3315567").Result;

            Assert.IsNotNull(result);
        }
    }
}