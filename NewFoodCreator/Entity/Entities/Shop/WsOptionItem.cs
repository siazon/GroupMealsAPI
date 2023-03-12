using System.Collections.Generic;

namespace Takeaway.Service.Contract.Entities.Shop
{
    public class WsOptionItem : WsEntity
    {
        public string ItemName { get; set; }
        public string ItemDescription { get; set; }
        public decimal? UnitPrice { get; set; }

        public string TranslateName { get; set; }

        public bool? Isactive { get; set; }

        public int? SortOrder { get; set; }

        public string DisplayName { get; set; }

        public int? GroupId { get; set; }
        public bool IsSelected { get; set; }



    }
}