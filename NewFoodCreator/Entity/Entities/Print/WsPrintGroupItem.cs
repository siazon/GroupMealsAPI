using System.Collections.Generic;
using Takeaway.Service.Contract.Entities.Shop;

namespace Takeaway.Service.Contract.Entities.Print
{
    public class WsPrintGroupItem: WsEntity
    {
        public int? Count { get; set; }
        public string Name { get; set; }
        public string TranslatedName { get; set; }
        
        public List<WsMenuItem> MenuItems { get; set; }
        public string ValuePart { get; set; }
        public int? SortOrder { get; set; }

        
    }
}