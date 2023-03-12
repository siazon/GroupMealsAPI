using System.Collections.Generic;
using System.Linq;

namespace App.Domain.TravelMeals.Restaurant
{
    public class TrDbRestaurant : DbEntity
    {
        public TrDbRestaurant()
        {
            ImageList = new List<string>();
            Categories = new List<TrDbRestaurantMenuCategory>();
        }

        public string StoreName { get; set; }
        public string StoreNameCn { get; set; }
        public string StoreNumber { get; set; }
        public string Description { get; set; }
        public string DescriptionCn { get; set; }
        public string DescriptionHtml { get; set; }
        public string DescriptionHtmlCn { get; set; }
        public string PhoneNumber { get; set; }
        public string Rating { get; set; }
        public string Website { get; set; }
        public string Image { get; set; }
        public string Image1 { get; set; }
        public string Image2 { get; set; }
        public string Image3 { get; set; }
        public string Image4 { get; set; }
        public string Image5 { get; set; }
        public string Image6 { get; set; }
        public string ShopAddress1 { get; set; }
        public string ShopAddress2 { get; set; }
        public string FoodCategory { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public string Email { get; set; }
        public string ContactEmail { get; set; }
        public string ShopOpenHours { get; set; }
        public string GoogleMap { get; set; }
        public string Features { get; set; }
        public string SpecialDiets { get; set; }
        public List<string> ImageList { get; set; }
        public int? BookingHourLength { get; set; }
        
        public List<TrDbRestaurantMenuCategory> Categories { get; set; }
    }

    public static class TrDbRestaurantExt
    {
        public static List<TrDbRestaurant> ClearForOutPut(this IEnumerable<TrDbRestaurant> source)
        {
            foreach (var item in source)
            {
                item.ContactEmail = "";
            }

            return source.ToList();
        }
    }

}