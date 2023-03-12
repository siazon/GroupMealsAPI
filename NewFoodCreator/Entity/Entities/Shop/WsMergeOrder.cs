namespace Takeaway.Service.Contract.Entities.Shop
{
    public class WsMergeOrder:WsEntity
    {
        public WsOrder CurrentOrder { get; set; }
        public WsOrder SelectedOrder { get; set; }
    }
}