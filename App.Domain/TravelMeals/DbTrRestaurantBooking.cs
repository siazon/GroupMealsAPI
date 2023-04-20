using App.Domain.TravelMeals.Restaurant;
using System;
using System.Collections.Generic;

namespace App.Domain.TravelMeals
{
    public class TrDbRestaurantBooking : DbEntity
    {
        public string RestaurantId { get; set; }
        public string RestaurantName { get; set; }
        public string RestaurantPhone { get; set; }
        public string RestaurantEmail { get; set; }
        public string RestaurantAddress { get; set; }
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public string CustomerPhone { get; set; }
        public string BookingDate { get; set; }
        public string BookingTime { get; set; }
        public int NumberOfAdults { get; set; }
        public int NumberOfChildren { get; set; }
        public string BookingNotes { get; set; }
        public DateTime? SelectDateTime { get; set; }
        public List<BookingCourses> Courses { get; set; } =new List<BookingCourses>();
        public string StripeProductId { get; set; }
        public string StripePriceId { get; set; }
        public string StripePaymentId { get; set; }
        public string StripeCustomerId { get; set; }
        public string StripeReceiptUrl { get; set; }
        public bool StripeSetupIntent{ get; set; }
        public string StripeClientSecretKey{ get; set; }
        public string BillReceipt { get; set; }
        public bool IsPaid { get; set; }
    }
    public class BookingCourses: TrDbRestaurantMenuCourse
    {
        public int qty { get; set; }
    }
}