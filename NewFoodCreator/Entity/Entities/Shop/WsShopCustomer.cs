namespace Takeaway.Service.Contract.Entities.Shop
{
    public class WsShopCustomer:WsEntity
    {

        public string ContactName { get; set; }

        /// <summary>
        /// Customer Phone number
        /// </summary>
        public string PhoneNumber { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Customer Address
        /// </summary>
        public string Address { get; set; }

        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        /// <summary>
        /// Delivery Charge
        /// </summary>
        public decimal? DeliveryCharge { get; set; }

        //Password for authentication
        public string Password { get; set; }


        //Authentication code for SMS
        public string AuthenticationCode { get; set; }


        public int? ValidationPending { get; set; }

        public int? IsFirstPurchase { get; set; }


        public int? MarketingPermission { get; set; }

        public bool? MarketingPermissionNew { get; set; }

        public int? RewardPoints { get; set; }


        public string UniqueRef { get; set; }


    }
}