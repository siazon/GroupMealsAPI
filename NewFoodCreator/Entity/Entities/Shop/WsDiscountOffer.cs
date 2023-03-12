namespace Takeaway.Service.Contract.Entities.Shop
{
    public class WsDiscountOffer : WsEntity
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal? DiscountValue { get; set; }
        public bool? Isactive { get; set; }
    }
}