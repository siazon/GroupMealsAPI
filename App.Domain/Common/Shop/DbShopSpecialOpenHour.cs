using System;

namespace App.Domain.Common.Shop
{
    public class DbShopSpecialOpenHour : DbEntity
    {
        public DateTime? SpecialDay { get; set; }

        //Calculated field

        public DateTime? ShopDeliveryStartTime { get; set; }
        public DateTime? ShopDeliveryEndTime { get; set; }
        public DateTime? ShopCollectionStartTime { get; set; }
        public DateTime? ShopCollectionEndTime { get; set; }
    }
}