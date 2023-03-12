namespace Takeaway.Service.Contract.Entities.Shop
{
    public class WsMenuItemCategory : WsEntity
    {
        /// <summary>
        /// Category Name
        /// </summary>
        public string Name { get; set; }

        public string DisplayName { get; set; }

        /// <summary>
        /// Category Description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Category Image Path
        /// </summary>
        public string WebImageUrl { get; set; }


        public bool? Isactive { get; set; }

        public bool? IsInternal { get; set; }

        public string DisplayExpression { get; set; }

        public int? CategoryGroupId { get; set; }

        public string TranslateName { get; set; }

        public int? SortOrder { get; set; }



    }
}