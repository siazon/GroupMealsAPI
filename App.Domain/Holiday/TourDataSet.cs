using App.Domain.Common.Shop;

namespace App.Domain.Holiday
{
    public class TourDataSet: DbEntity
    {
        public TourBooking Booking { get; set; }
        public DbShop ShopInfo { get; set; }
    }
}