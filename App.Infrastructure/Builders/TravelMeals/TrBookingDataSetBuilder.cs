using App.Domain.Common.Shop;
using App.Domain.TravelMeals;
using App.Domain.TravelMeals.Restaurant;
using System;

namespace App.Infrastructure.Builders.TravelMeals
{
    public interface ITrBookingDataSetBuilder
    {
        TrDbRestaurantBooking BuildTravelMealContent(DbShop shopInfo, TrDbRestaurant restaurant, TrDbRestaurantBooking booking);
    }

    public class TrBookingDataSetBuilder : ITrBookingDataSetBuilder
    {
        public TrDbRestaurantBooking BuildTravelMealContent(DbShop shopInfo, TrDbRestaurant restaurant, TrDbRestaurantBooking booking)
        {
            var emailDataSet = new TrDbRestaurantBooking
            {
                ShopName = restaurant.StoreName,
                PhoneNumber = restaurant.StoreNumber,
                Address = restaurant.ShopAddress1,
                ShopEmail = restaurant.Email,
                NumberOfAdults = booking.NumberOfAdults,
                BookingDate = GetBookingDateTime(booking.SelectDateTime),
            };

            return emailDataSet;
        }

        private string GetBookingDateTime(DateTime? bookingTime)
        {
            return bookingTime?.ToString("dd-MMM-yyyy HH:mm") ?? string.Empty;
        }

        private string GetBookingTime(DateTime? bookingTime)
        {
            return bookingTime?.ToString("HH:mm") ?? string.Empty;
        }

        private string GetBookingDate(DateTime? bookingDate)
        {
            return bookingDate?.ToString("dd-MMM-yyyy") ?? string.Empty;
        }

        private string GetNumberOfChildren(int? numberOfChilds)
        {
            return numberOfChilds?.ToString() ?? "0";
        }
    }
}