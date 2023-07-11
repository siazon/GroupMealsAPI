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
            foreach (var item in booking.Details)
            {
                for (int i = 0; i < item.Courses.Count; i++)
                {
                    TrDbRestaurantMenuCourse menuCourse = item.Courses[i];
                    var course = restaurant.Categories.FirstOrDefault(a => a.Id == menuCourse.Id);
                    menuCourse.MenuItems = course.MenuItems;
                    menuCourse.RestaurantId = course.RestaurantId;
                    menuCourse.CourseName = course.CourseName;
                    menuCourse.CourseDescription = course.CourseDescription;
                    menuCourse.CourseDescriptionCn = course.CourseDescriptionCn;
                    menuCourse.CourseNameCn = course.CourseNameCn;
                    menuCourse.CourseName = course.CourseName.ToString();
                }
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