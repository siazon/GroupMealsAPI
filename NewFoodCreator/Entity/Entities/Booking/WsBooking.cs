using System;

namespace Takeaway.Service.Contract.Entities.Booking
{
    public class WsBooking:WsEntity
    {
        public string ContactName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }

        public int? NumberOfAdults { get; set; }
        public int? NumberOfChilds { get; set; }

        public DateTime? BookingDate { get; set; }
        public DateTime? BookingTime { get; set; }
    }
}