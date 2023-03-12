using System.Collections.Generic;

namespace App.Domain.TravelMeals.Restaurant
{
    public class TrDbRestaurantMenuCategory : DbEntity
    {
        public TrDbRestaurantMenuCategory()
        {
            MenuItems = new List<TrDbRestaurantMenuItem>();
        }

        public string CategoryName { get; set; }
        public string CategoryNameCn { get; set; }
        public string CategoryDescription { get; set; }
        public string CategoryDescriptionCn { get; set; }
        public int? RestaurantId { get; set; }

        public List<TrDbRestaurantMenuItem> MenuItems { get; set; }
    }
}