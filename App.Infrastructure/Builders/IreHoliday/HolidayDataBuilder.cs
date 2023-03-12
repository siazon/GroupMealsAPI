using App.Domain.Common.Shop;
using App.Domain.Holiday;

namespace App.Infrastructure.Builders.IreHoliday
{
    public interface IHolidayDataBuilder
    {
        TourDataSet BuildContent(DbShop shopInfo, TourBooking booking);
    }
    public class HolidayDataBuilder: IHolidayDataBuilder
    {
        public TourDataSet BuildContent(DbShop shopInfo, TourBooking booking)
        {
            var emailDataSet = new TourDataSet
            {
                ShopInfo = shopInfo,
                Booking = booking,
                ShopId = shopInfo.ShopId
            };

            return emailDataSet;
        }
    }
}