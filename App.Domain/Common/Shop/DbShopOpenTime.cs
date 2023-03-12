using System;
using System.Collections.Generic;

namespace App.Domain.Common.Shop
{
    public class DbShopOpenTime: DbEntity
    {
        /// Calculated fields
        public bool? IsOpen { get; set; }

        /// Calculated fields
        public DateTime? ShopOpenTime { get; set; }
        public DateTime? ShopCloseTime { get; set; }

        public DateTime? ShopDeliveryStartTime { get; set; }
        public DateTime? ShopDeliveryEndTime { get; set; }
        public DateTime? ShopCollectionStartTime { get; set; }
        public DateTime? ShopCollectionEndTime { get; set; }
        public List<string> CollectionOptions { get; set; }
        public List<string> DeliveryOptions { get; set; }
    }
}