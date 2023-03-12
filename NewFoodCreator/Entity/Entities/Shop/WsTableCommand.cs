namespace Takeaway.Service.Contract.Entities.Shop
{
    public class WsTableCommand: WsEntity
    {
        public string ItemName { get; set; }
        public string DisplayName { get; set; }
        public string ItemDescription { get; set; }
       
        public string TranslateName { get; set; }

        public bool? Isactive { get; set; }

        public int? SortOrder { get; set; }


        public bool IsSelected { get; set; }
    }
}