using System.Collections.Generic;

namespace Takeaway.Service.Contract.Entities.Shop
{
    public class WsOrderItems: WsEntity
    {
        public WsMenuItem OrderItem { get; set; }
        public int Quantity { get; set; }

        public string SelectionContent { get; set; }

        
    }
}