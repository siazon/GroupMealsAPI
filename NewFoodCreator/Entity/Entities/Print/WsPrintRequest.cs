using System.Collections.Generic;
using Takeaway.Service.Contract.Entities.Shop;

namespace Takeaway.Service.Contract.Entities.Print
{
    public class WsPrintRequest : WsEntity
    {

        public string PrinterName { get; set; }
        public string DocumentName { get; set; }
        public string PrintMode { get; set; }
        public int BatchId { get; set; }

        public WsTables TableInfo { get; set; }
        public WsOrder Order { get; set; }
        public List<WsMenuItem> OrderItems { get; set; }
        public WsShop ShopInfo { get; set; }

        public List<WsPrintRawData> RawDatas { get; set; }


        public WsPrintRequest()
        {
            RawDatas = new List<WsPrintRawData>();
        }
    }
}