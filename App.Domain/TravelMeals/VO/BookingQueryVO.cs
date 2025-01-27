using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Domain.TravelMeals.VO
{
  
    public class BookingQueryVO
    {
        public string content { get; set; }
        public int filterTime { get; set; }
        public DateTime stime { get; set; }
        public DateTime etime { get; set; }
        public int status { get; set; }
        public int pageSize { get; set; }
        public string continuationToken { get; set; }

    }
    public class BookingQueryRestaurantVO: BookingQueryVO
    {

        public List<int> status { get; set; }
    }
}
