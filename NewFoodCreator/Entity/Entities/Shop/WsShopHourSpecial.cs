using System;

namespace Takeaway.Service.Contract.Entities.Shop
{
    public class WsShopHourSpecial : WsEntity
    {
        public DateTime? SpecialDate { get; set; }

        public DateTime? StartTime { get; set; }

        public DateTime? FinishTime { get; set; }

        public bool? IsClosed { get; set; }
    }
}