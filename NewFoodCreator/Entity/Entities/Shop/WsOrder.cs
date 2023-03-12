using System;
using System.Collections.Generic;
using Takeaway.Service.Contract.Entities.Payment;

namespace Takeaway.Service.Contract.Entities.Shop
{
    public class WsOrder : WsEntity
    {
        /// <summary>
        /// Items on the Order
        /// </summary>
        public List<WsMenuItem> OrderItems { get; set; }

        /// <summary>
        ///
        /// </summary>
        public string DiscountRules { get; set; }

        /// <summary>
        /// Total Amount of the Order
        /// </summary>
        public decimal OrderTotal { get; set; }

        public decimal? ServiceCharge { get; set; }

        /// <summary>
        /// Total Amount of the Order
        /// </summary>
        public decimal OrderOriginalTotal { get; set; }

        /// <summary>
        /// Customer Contact Name
        /// </summary>
        public string ContactName { get; set; }

        /// <summary>
        /// Customer Phone number
        /// </summary>
        public string PhoneNumber { get; set; }

        /// <summary>
        ///
        /// </summary>
        public string Email { get; set; }
        public string IpAddress { get; set; }

        /// <summary>
        /// Customer Address
        /// </summary>
        public string Address { get; set; }

        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        /// <summary>
        /// Order Comment
        /// </summary>
        public string Comment { get; set; }

        /// <summary>
        /// Status Id
        /// </summary>
        public int? StatusId { get; set; }

        /// <summary>
        /// If the order is internal
        /// </summary>
        public bool? IsInternalOrder { get; set; }

        public bool? IsWalkIn { get; set; }

        public bool? IsMobileOrder { get; set; }
        public int? TableNumber { get; set; }
        public int? NumberOfPeople { get; set; }
        public int? OrderSource { get; set; }

        public DateTime? Created { get; set; }

        /// <summary>
        /// Delivery type delivery or collection
        /// </summary>
        public WsDeliveryOption DeliveryType { get; set; }

        public decimal DeliveryCharge { get; set; }

        public string PaymentRef { get; set; }

        public string OrderRef { get; set; }

        public string PaymentType { get; set; }

        public string DriverName { get; set; }

        public int AgentId { get; set; }

        public List<WsTableCommand> TableCommand { get; set; }

        public bool? IsModified { get; set; }
        public bool? IsUploaded { get; set; }

        public string CancellationReason { get; set; }

        public int? MarketingPermission { get; set; }

        public bool? MarketingPermissionNew { get; set; }

        public string CollectionTime { get; set; }
        public int? RewardPoints { get; set; }

        public WsPayment OrderPayment { get; set; }
        
        public WsCheckout Checkout { get; set; }

        public WsOrder()
        {
            OrderItems = new List<WsMenuItem>();
            TableCommand = new List<WsTableCommand>();
        }
    }
}