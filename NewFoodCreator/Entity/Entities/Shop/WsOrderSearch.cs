using System;

namespace Takeaway.Service.Contract.Entities.Shop
{
    public class WsOrderSearch:WsEntity
    {
        public DateTime? SearchDate { get; set; }
        public string SearchPhone { get; set; }
        public string SearchAddress { get; set; }
        public string SearchRef { get; set; }
    }
}