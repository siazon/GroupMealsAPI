using System;

namespace App.Domain.TravelMeals.Trip
{
    public class DbTripBooking : DbEntity
    {
        //Book Start Date
        public DateTime? BookDate { get; set; }

        public int? NumberOfAdults { get; set; }

        public string ContactName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }

        public int? NumberOfChild { get; set; }

        public DbTrip Trip { get; set; }
        public DateTime? BookingTime { get; set; }

        //TripBookingStatusEnum
        public int? BookingStatus { get; set; }
    }
}