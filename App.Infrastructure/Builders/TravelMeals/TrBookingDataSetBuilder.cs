using App.Domain.Common.Shop;
using App.Domain.TravelMeals;
using App.Domain.TravelMeals.Restaurant;
using System;
using System.Linq;

namespace App.Infrastructure.Builders.TravelMeals
{
    public interface ITrBookingDataSetBuilder
    {
        TrDbRestaurantBooking BuildTravelMealContent(TrDbRestaurant restaurant, TrDbRestaurantBooking booking);
    }

    public class TrBookingDataSetBuilder : ITrBookingDataSetBuilder
    {
        public TrDbRestaurantBooking BuildTravelMealContent(TrDbRestaurant restaurant, TrDbRestaurantBooking booking)
        {
            booking.BookingDate = GetBookingDate(booking.SelectDateTime);
            booking.BookingTime = GetBookingTime(booking.SelectDateTime);

           for(int i=0;i<booking.Courses.Count;i++)
            {
                TrDbRestaurantMenuCourse item = booking.Courses[i];
                var course= restaurant.Categories.FirstOrDefault(a=>a.Id==item.Id);
                item.MenuItems=course.MenuItems;
                item.RestaurantId=course.RestaurantId;
                item.CourseName=course.CourseName;
                item.CourseDescription=course.CourseDescription;
                item.CourseDescriptionCn=course.CourseDescriptionCn;
                item.CourseNameCn=course.CourseNameCn;
                item.CourseName=course.CourseName.ToString();
            }
            return booking;
        }

        private string GetBookingDateTime(DateTime? bookingTime)
        {
            return bookingTime?.ToString("dd-MM-yyyy HH:mm") ?? string.Empty;
        }

        private string GetBookingTime(DateTime? bookingTime)
        {
            return bookingTime?.ToString("HH:mm") ?? string.Empty;
        }

        private string GetBookingDate(DateTime? bookingDate)
        {
            return bookingDate?.ToString("dd-MM-yyyy") ?? string.Empty;
        }

        private string GetNumberOfChildren(int? numberOfChilds)
        {
            return numberOfChilds?.ToString() ?? "0";
        }
    }
}