namespace Takeaway.Service.Contract.Entities.Shop
{
    public class WsDeliveryArea: WsEntity
    {
        public string Area { get; set; }
        public decimal? MinOrder { get; set; }
        public decimal? DeliveryCost { get; set; }
        public int? DeliveryGroup { get; set; }
    }
}