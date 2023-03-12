using System;

namespace App.Domain.Common.Shop
{
    public class DbShopGeneralOpenHour : DbEntity
    {
        public string DayOfWeek { get; set; }


        public DateTime? ShopDeliveryStartTime { get; set; }
        public DateTime? ShopDeliveryEndTime { get; set; }
        public DateTime? ShopCollectionStartTime { get; set; }
        public DateTime? ShopCollectionEndTime { get; set; }
    }
}