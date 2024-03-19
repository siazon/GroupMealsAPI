using App.Domain.Common.Shop;
using App.Domain.TravelMeals;
using App.Infrastructure.Builders.Common;
using App.Infrastructure.Repository;
using App.Infrastructure.ServiceHandler.TravelMeals;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Infrastructure.Utility.Common
{
    public class QuartzTask : IJob
    {
        
        public string BookingID { get; set; }
        public DbShop ShopInfo { get; set; }
        public string TempName { get; set; }
        public string Subject { get; set; }
        public string WwwPath { get; set; }
        public IContentBuilder ContentBuilder { get; set; }
        public ILogManager Logger { get; set; }
        public IDbCommonRepository<TrDbRestaurantBooking> RestaurantBookingRepository { get; set; }
        public Task Execute(IJobExecutionContext context)
        {
            return Task.Factory.StartNew(async () =>
            {
                var booking = await RestaurantBookingRepository.GetOneAsync(a => a.Id == BookingID);
                EmailUtils.EmailCustomer(booking, ShopInfo, TempName, WwwPath, Subject, ContentBuilder, Logger);
            });
        }
    }
}
