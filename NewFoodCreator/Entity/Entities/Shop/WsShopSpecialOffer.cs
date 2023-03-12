

using System;

namespace Takeaway.Service.Contract.Entities.Shop
{

    public class WsShopSpecialOffer : WsEntity
    {

        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime? Created { get; set; }

         
    }
}