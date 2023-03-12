using System.Collections.Generic;

namespace App.Domain.TravelMeals.Store
{
    public class DbStore : DbEntity
    {
        public string StoreName { get; set; }
        public string StoreNumber { get; set; }
        public string Website { get; set; }
        public string ShopMenuLink { get; set; }
        public string ShopFacebook { get; set; }
        public string ShopLinkedIn { get; set; }
        public string ShopInstagram { get; set; }
        public string ShopAddress1 { get; set; }
        public string ShopAddress2 { get; set; }
        public string Email { get; set; }
        public string ContactEmail { get; set; }
        public List<string> ImageList { get; set; }
        public string ShopOpenHours { get; set; }
        public string GoogleMap { get; set; }
        public int? BookingHourLength { get; set; }
        public bool? IsActive { get; set; }
    }
}