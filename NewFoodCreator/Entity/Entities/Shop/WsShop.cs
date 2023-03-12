using System;
using System.Collections.Generic;

namespace Takeaway.Service.Contract.Entities.Shop
{
    public class WsShop : WsEntity
    {
        public string ShopName { get; set; }

        public string ShopNumber { get; set; }

        public string ShopNumber2 { get; set; }

        public string ShopMobile { get; set; }

        public string ShopUrl { get; set; }

        public string ShopMenuLink { get; set; }

        public string ShopAddress1 { get; set; }

        public string ShopAddress2 { get; set; }

        public string Email { get; set; }

        public string ShopBackGroundImagePath { get; set; }

        public string ShopSpecialOffer { get; set; }

        public bool? IsOpen { get; set; }

        public bool? IsOpenByOwner { get; set; }

        public decimal? MinOrder { get; set; }

        public decimal? MaxOrder { get; set; }

        public decimal? DeliveryCharge { get; set; }

        public decimal? ServiceCharge { get; set; }

        public string ShopOpenHours { get; set; }

        public bool? TakePayment { get; set; }
        public string StripeKey { get; set; }

        public string ShopDeliveryOpenHours { get; set; }
        public string GoogleMap { get; set; }

        public string IoSlink { get; set; }
        public string Androidlink { get; set; }

        public bool? Isactive { get; set; }

        public int? SmsBalance { get; set; }
        public string CountryCode { get; set; }
        public string Currency { get; set; }

        public DateTime? ShopOpenTime { get; set; }
        public DateTime? ShopDeliveryStartTime { get; set; }
        public DateTime? ShopDeliveryEndTime { get; set; }
        public DateTime? ShopCollectionStartTime { get; set; }
        public DateTime? ShopCollectionEndTime { get; set; }

        public List<string> CollectionOptions { get; set; }
        public List<string> DeliveryOptions { get; set; }

        public int? ShopCollectionState { get; set; }
        public int? ShopDeliveryState { get; set; }

        public bool? ActiveSync { get; set; }
        public decimal? RegisterDiscount { get; set; }

        public int? RequireTestOrder { get; set; }

        public List<WsShopNews> ShopNews { get; set; }

        public int? DeliveryMin { get; set; }
        public int? CollectionMin { get; set; }
        public int? BookingMin { get; set; }
        public string PromotionBannerText { get; set; }

        public string FacebookLink { get; set; }
        public string PromotionWeb { get; set; }

        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        public string Statement { get; set; }
        public bool? DeliveryOnly { get; set; }
        public bool? PaycardOnly { get; set; }
        public bool? CollectionOnly { get; set; }

        public WsShop()
        {
            ShopNews = new List<WsShopNews>();
            CollectionOptions = new List<string>();
            DeliveryOptions = new List<string>();
        }
    }
}