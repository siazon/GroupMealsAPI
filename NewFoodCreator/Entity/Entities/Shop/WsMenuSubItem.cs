using System.Collections.Generic;

namespace Takeaway.Service.Contract.Entities.Shop
{
    public class WsMenuSubItem : WsEntity
    {
        /// <summary>
        /// Sub Item Name
        /// </summary>
        public string Name { get; set; }

     
        /// <summary>
        /// Sub Item Description
        /// </summary>
        public string Description { get; set; }

        public int? SortOrder { get; set; }
        public int? GroupId { get; set; }
        public int? OptionItemGroupId { get; set; }

        /// <summary>
        /// Sub Item selection Choises
        /// </summary>
        public List<WsMenuItem> Selections { get; set; }

        /// <summary>
        /// Sub Item selected Choise 
        /// </summary>
        public WsMenuItem Selected { get; set; }

        public int SelectedId { get; set; }

        public WsMenuSubItem()
        {
            Selections = new List<WsMenuItem>();
        }
    }
}