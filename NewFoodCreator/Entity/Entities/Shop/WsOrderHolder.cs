using System;

namespace Takeaway.Service.Contract.Entities.Shop
{
    public class WsOrderHolder:WsEntity
    {
        public DateTime? HoldTime { get; set; }

        public WsOrder Order { get; set; }

    }
}