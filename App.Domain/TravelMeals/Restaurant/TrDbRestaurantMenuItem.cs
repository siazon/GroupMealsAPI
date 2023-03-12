namespace App.Domain.TravelMeals.Restaurant
{
    public class TrDbRestaurantMenuItem : DbEntity
    {
        public string MenuItemName { get; set; }
        public string MenuItemNameCn { get; set; }
        public string MenuItemDescription { get; set; }
        public string MenuItemDescriptionCn { get; set; }
        public int? CategoryId { get; set; }
    }
}