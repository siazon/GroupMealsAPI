using System;

namespace Takeaway.Service.Contract.Entities.Shop
{
    public class WsShopHour : WsEntity
    {
        public string DayOfWeek { get; set; }

        public DateTime? StartTime { get; set; }

        public DateTime? FinishTime { get; set; }
        public DateTime? DeliveryStartTime { get; set; }



    }
}