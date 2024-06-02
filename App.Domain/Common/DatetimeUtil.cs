using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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

    public static class ExtensionMethods
    {
        public static List<T> FindAll<T>(this List<T> list, List<Predicate<T>> predicates)
        {
            List<T> L = new List<T>();
            foreach (T item in list)
            {
                bool pass = true;
                foreach (Predicate<T> p in predicates)
                {
                    if (!(p(item)))
                    {
                        pass = false;
                        break;
                    }
                }
                if (pass) L.Add(item);
            }
            return L;
        }


     

    }

}


