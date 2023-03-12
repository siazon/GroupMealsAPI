using System.Collections.Generic;

namespace Takeaway.Service.Contract.Entities.Shop
{
    public class WsMenuItem : WsEntity
    {
        /// <summary>
        /// Item name
        /// </summary>
        public string Name { get; set; }

        public string DisplayName { get; set; }

        public string TranslatedName { get; set; }

        /// <summary>
        /// Item Original Price
        /// </summary>
        public decimal? Price { get; set; }

      
        /// <summary>
        /// Description of the item
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Comment added by users
        /// </summary>
        public string Comment { get; set; }

        /// <summary>
        /// Image Path of the item
        /// </summary>
        public string ImageUrl { get; set; }


        /// <summary>
        /// Video Links
        /// </summary>
        public string VideoLink { get; set; }

        /// <summary>
        /// If item has Sub Orders
        /// </summary>
        public bool? HasSubOrder { get; set; }

        public bool? Isactive { get; set; }
        public bool? Ishot { get; set; }
        public int? HotGroupId { get; set; }
        public int? SortOrder { get; set; }

        public int? MenuItemCategoryId { get; set; }

        public bool? NoSelection { get; set; }

        public bool? ExcludeOnline { get; set; }

        public int? FoodCategoryId { get; set; }

        public int? BatchId { get; set; }
        public string ExtraComment { get; set; }

        public int? RewardPoints { get; set; }

        /// <summary>
        /// List of sub items to select from
        /// </summary>
        public List<WsMenuSubItem> SubItemSelections { get; set; }

        public WsMenuItem()
        {
            SubItemSelections = new List<WsMenuSubItem>();
        }

    }
}