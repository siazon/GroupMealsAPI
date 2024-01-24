using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Domain.TravelMeals.Restaurant
{
    public class CartInfo
    {
        public string RestaurantId { get; set; }
        public string RestaurantName { get; set; }
        public string Currency { get; set; }
        public string Memo { get; set; }
        public DateTime MealTime { get; set; }
        public RestaurantBillInfo BillInfo { get; set; }
        public List<CourseInfo> Courses { get; set; } = new List<CourseInfo>();
    }
    public class CourseInfo {
        public string id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int Qty { get; set; }
    }
}
