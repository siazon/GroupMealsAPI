using App.Domain.Holiday;
using GenFu;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using App.Infrastructure.Builders.Common;

namespace App.UnitTest.Domain
{
    [TestClass]
    public class TourTest
    {
        [TestMethod]
        public void Create()
        {
            var jsonTourBooking = Resource1.objJSon;

            var booking = JsonConvert.DeserializeObject<TourBooking>(jsonTourBooking);

            var dataSet = new TourDataSet();
            dataSet.Booking = booking;

            var emailTemplate = Resource1.irebooking;

            var result = new ContentBuilder().BuildRazorContent(dataSet, emailTemplate);


            var final = result.Result;


            Assert.IsNotNull(booking);
            
        }
    }
}