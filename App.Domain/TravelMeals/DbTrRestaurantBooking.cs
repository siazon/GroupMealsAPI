using System;

namespace App.Domain.TravelMeals
{
    public class TrDbRestaurantBooking : DbEntity
    {
        public string RestaurantId { get; set; }
        public string RestaurantName { get; set; }
        public int NumberOfAdults { get; set; }
        public string NumberOfChilds { get; set; }
        public string BookingNotes { get; set; }
        public DateTime? SelectDateTime { get; set; }
        public string ShopName { get; set; }
        public string ContactName { get; set; }
        public string UserEmail { get; set; }
        public string CustomerPhone { get; set; }
        public string Address { get; set; }
        public string PhoneNumber { get; set; }
        public string ShopEmail { get; set; }
        public string BookingDate { get; set; }
        public string BookingTime { get; set; }
    }
}