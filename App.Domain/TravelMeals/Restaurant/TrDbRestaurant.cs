using App.Domain.Common.Shop;
using App.Domain.Enum;
using App.Domain.TravelMeals.VO;
using System;
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
        public string ContactPhone { get; set; }
        public string Wechat { get; set; }
        public int Rating { get; set; }
        public RestaurantCategoryEnum FoodCategory { get; set; }
        public List<RestaurantCategoryEnum> FoodCategories { get; set; }
        public RestaurantTagEnum RestaurantTag { get; set; }
        public string Website { get; set; }
        public string Image { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string TimeZone { get; set; }
        public string Country { get; set; }
        public string Currency { get; set; }
        public double Vat { get; set; }
        public string Email { get; set; }
        public string ContactEmail { get; set; }
        public string SupportName { get; set; }
        public string SupportEmail { get; set; }
        public string SupportPhone { get; set; }
        public string OpenHours { get; set; }
        public string GoogleMap { get; set; }
        public MapPosition MapPosition { get; set; }
        public List<int> Features { get; set; }
        public string SpecialDiets { get; set; }
        public string ParkingLot { get; set; }
        public string CancelCondition { get; set; }
        public int ParkingCapacity { get; set; }
        public List<string> Images { get; set; }
        public List<string> Comments { get; set; }
        public List<string> Tags { get; set; }
        public List<string> Attractions { get; set; }
        public int? BookingHourLength { get; set; }
        public int MinGuest { get; set; }
        public int MaxGuest { get; set; }
        public int SpeakerFlag { get; set; }
        public bool VegetarianDiet { get; set; }
        public bool HalalFood { get; set; }
        public bool DriverFree { get; set; }
        public decimal Price { get; set; }
        public decimal PriceIncrease { get; set; }
        public decimal ChildrenPrice { get; set; }
        public bool IncluedVAT { get; set; }
        public bool ShowPaid { get; set; }

        public RestaurantBillInfo BillInfo { get; set; }

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
                Address = this.Address,
                FoodCategory = this.FoodCategory,
                City = this.City,
                Country = this.Country,
                TimeZone = this.TimeZone,
                Currency= this.Currency,
                Vat= this.Vat,
                Email = this.Email,
                ContactEmail = this.ContactEmail,
                OpenHours = this.OpenHours,
                GoogleMap = this.GoogleMap,
                Features = this.Features,
                SpecialDiets = this.SpecialDiets,
                Images = new List<string>(this.Images),
                BookingHourLength = this.BookingHourLength,
                Categories = this.Categories.Select(c => c.Clone()).ToList()
            };
        }
    }
    public enum MenuCalculateTypeEnum
    {
        /// <summary>
        /// 默认中餐
        /// </summary>
        DEFAULT, WesternFood, ChineseFood
    }
    public enum PaymentTypeEnum
    {
        Full, Percentage, Fixed
    }
    public class RestaurantBillInfo
    {
        public List<PaymentTypeEnum> SupportedPaymentTypes { get; set; }//1全额，2全额+到店支付，3全额+到店支付+百分比支付
        public PaymentTypeEnum PaymentType { get; set; }//支付方式 全额支付，百分百，固定金额
        public double PayRate { get; set; }//百分比
        public bool IsOldCustomer { get; set; }
        public PaymentTypeEnum RewardType { get; set; } = PaymentTypeEnum.Percentage;//无返现，百分比，固定
        public double Reward { get; set; }

    }
    public class PaymentAmountInfo
    { 
        public decimal TotalPayAmount { get; set; }
        public string AmountText { get; set; }
        public string RewardText {  get; set; }
        public string UnPaidAmountText { get; set; }
        public Dictionary<string, decimal> AmountList { get; set; } = new Dictionary<string, decimal>();
        public Dictionary<string, decimal> UnPaidAmountList { get; set; } = new Dictionary<string, decimal>();

        public List<IntentTypeEnum> IntentType { get; set; } = new List<IntentTypeEnum>();

    }
    public class ItemPayInfo {
        public decimal PayAmount { get; set; }
        public decimal Reward { get; set; }
        public decimal Vat { get; set; }
        public decimal Commission { get; set; }
    }
    public class MapPosition
    {
        public double lat { get; set; }
        public double lng { get; set; }
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