namespace Takeaway.Service.Contract.Entities.Shop
{
    public class WsOptionItemSelection:WsEntity
    {
        public WsOptionItem OptionItem { get; set; }
        public int AddCommand { get; set; }
        public decimal Cost { get; set; }
        public string Comment { get; set; }

    }
}