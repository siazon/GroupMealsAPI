using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Domain.Common
{
    public static class DatetimeUtil
    {
        public static string GetBookingTime(this DateTime? bookingTime)
        {
            return bookingTime?.ToString("HH:mm") ?? string.Empty;
        }

        public static string GetBookingDate(this DateTime? bookingDate)
        {
            return bookingDate?.ToString("dd-MM-yyyy") ?? string.Empty;
        }
    }
}
