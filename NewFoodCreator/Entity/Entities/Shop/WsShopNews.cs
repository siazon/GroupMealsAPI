using System;

namespace Takeaway.Service.Contract.Entities.Shop
{
    public class WsShopNews : WsEntity
    {
        public string News { get; set; }
        public DateTime? Created { get; set; }
    }
}