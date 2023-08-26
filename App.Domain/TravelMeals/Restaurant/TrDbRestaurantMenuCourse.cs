using App.Domain.Enum;
using System.Collections.Generic;
using System.Linq;

namespace App.Domain.TravelMeals.Restaurant
{
    public class TrDbRestaurantMenuCourse : DbEntity
    {
        public TrDbRestaurantMenuCourse()
        {
            MenuItems = new List<TrDbRestaurantMenuItem>();
        }

        public string CourseName { get; set; }
        public string CourseNameCn { get; set; }
        public string CourseDescription { get; set; }
        public string CourseDescriptionCn { get; set; }
        public int? RestaurantId { get; set; }
        public bool IsChildCourse { get; set; }
        /// <summary>
        /// 0:普通，1:  4-5 ： 0.8优惠, 6-7 ： 0.85优惠，8：0.9优惠, 9: 0.95优惠
        /// </summary>
        public int CourseType { get; set; }

        public List<TrDbRestaurantMenuItem> MenuItems { get; set; }
        public TrDbRestaurantMenuCourse Clone()
        {
            return new TrDbRestaurantMenuCourse
            {
                CourseName = this.CourseName,
                CourseNameCn = this.CourseNameCn,
                CourseDescription = this.CourseDescription,
                CourseDescriptionCn = this.CourseDescriptionCn,
                RestaurantId = this.RestaurantId,
                IsChildCourse = this.IsChildCourse,
                MenuItems = this.MenuItems.Select(m => m.Clone()).ToList()
            };
        }
    }
}