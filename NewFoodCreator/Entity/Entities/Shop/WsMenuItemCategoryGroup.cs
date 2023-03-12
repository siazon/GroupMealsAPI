namespace Takeaway.Service.Contract.Entities.Shop
{
    public class WsMenuItemCategoryGroup : WsEntity
    {
        public string Name { get; set; }
        public string Description { get; set; }

        public bool? Isactive { get; set; }


        public string TranslateName { get; set; }


        public int? SortOrder { get; set; }
        
         
    }
}