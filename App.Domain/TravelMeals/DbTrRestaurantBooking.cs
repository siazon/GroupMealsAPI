using App.Domain.Enum;
using App.Domain.TravelMeals.Restaurant;
using System;
using System.Collections.Generic;

namespace App.Domain.TravelMeals
{
    public class TrDbRestaurantBooking : StripeBase
    {
       
        public OrderStatusEnum Status { get; set; } = OrderStatusEnum.None;
        public int Accepted { get; set; }
        public string AcceptReason { get; set; }
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public string CustomerPhone { get; set; }
        public string BookingDate { get; set; } = DateTime.Now.ToString("yyyy-MMM-dd");
        public string BookingTime { get; set; } = DateTime.Now.ToString("HH:mm");
        public int NumberOfAdults { get; set; }
        public int NumberOfChildren { get; set; }
        public string BookingNotes { get; set; }
        public List<BookingDetail> Details { get; set; }


    }
    public class BookingDetail
    {
        public string RestaurantId { get; set; }
        public string RestaurantName { get; set; }
        public string RestaurantPhone { get; set; }
        public string RestaurantEmail { get; set; }
        public string RestaurantAddress { get; set; }
        public DateTime? SelectDateTime { get; set; }
        public List<BookingCourses> Courses { get; set; } = new List<BookingCourses>();
    }
    public class BookingCourses : TrDbRestaurantMenuItem
    {
        public int Qty { get; set; }
        public int TableQty { get; set; }
        public string Memo { get; set; }
        public decimal Amount { get; set; }
    }
}