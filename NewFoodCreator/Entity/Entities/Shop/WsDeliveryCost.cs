namespace Takeaway.Service.Contract.Entities.Shop
{
    public class WsDeliveryCost: WsEntity
    {
        
        public decimal? FromDistance { get; set; }
        public decimal? ToDistance { get; set; }

        public decimal? DeliveryCostAmount { get; set; }
        
    }
}