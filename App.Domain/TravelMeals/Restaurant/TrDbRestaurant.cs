using App.Domain.Common.Shop;
using App.Domain.Enum;
using System.Collections.Generic;
using System.Linq;

namespace App.Domain.TravelMeals.Restaurant
{
    public class TrDbRestaurant : DbEntity
    {
        public TrDbRestaurant()
        {
            Images = new List<string>();
            Categories = new List<TrDbRestaurantMenuCourse>();
            Comments = new List<string>();
        }

        public string StoreName { get; set; }
        public string StoreNameCn { get; set; }
        public string StoreNumber { get; set; }
        public string Description { get; set; }
        public string DescriptionCn { get; set; }
        public string DescriptionHtml { get; set; }
        public string DescriptionHtmlCn { get; set; }
        public string PhoneNumber { get; set; }
        public string Wechat { get; set; }
        public int Rating { get; set; }
        public RestaurantCategoryEnum FoodCategory { get; set; }
        public RestaurantTagEnum RestaurantTag { get; set; }
        public string Website { get; set; }
        public string Image { get; set; }
        public string ShopAddress { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public string Email { get; set; }
        public string ContactEmail { get; set; }
        public string ShopOpenHours { get; set; }
        public string GoogleMap { get; set; }
        public string Features { get; set; }
        public string SpecialDiets { get; set; }
        public string ParkingLot { get; set; }
        public string CancelCondition { get; set; }
        public int ParkingCapacity { get; set; }
        public List<string> Images { get; set; }
        public List<string> Comments { get; set; }
        public List<string> Tags { get; set; }
        public List<string> Attractions { get; set; }
        public int? BookingHourLength { get; set; }


        public List<TrDbRestaurantMenuCourse> Categories { get; set; }

        public TrDbRestaurant Clone()
        {
            return new TrDbRestaurant
            {
                StoreName = this.StoreName,
                StoreNameCn = this.StoreNameCn,
                StoreNumber = this.StoreNumber,
                Description = this.Description,
                DescriptionCn = this.DescriptionCn,
                DescriptionHtml = this.DescriptionHtml,
                DescriptionHtmlCn = this.DescriptionHtmlCn,
                PhoneNumber = this.PhoneNumber,
                Rating = this.Rating,
                Website = this.Website,
                Image = this.Image,
                ShopAddress = this.ShopAddress,
                FoodCategory = this.FoodCategory,
                City = this.City,
                Country = this.Country,
                Email = this.Email,
                ContactEmail = this.ContactEmail,
                ShopOpenHours = this.ShopOpenHours,
                GoogleMap = this.GoogleMap,
                Features = this.Features,
                SpecialDiets = this.SpecialDiets,
                Images = new List<string>(this.Images),
                BookingHourLength = this.BookingHourLength,
                Categories = this.Categories.Select(c => c.Clone()).ToList()
            };
        }
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