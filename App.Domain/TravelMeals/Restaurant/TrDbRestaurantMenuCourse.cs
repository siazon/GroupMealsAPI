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
        /// 0:��ͨ��1:  4-5 �� 0.8�Ż�, 6-7 �� 0.85�Żݣ�8��0.9�Ż�, 9: 0.95�Ż�
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