using App.Domain.Enum;
using Newtonsoft.Json;

namespace App.Domain.TravelMeals.Restaurant
{
    public class TrDbRestaurantMenuItem 
    {
        public string Id { get; set; }
        public string MenuItemName { get; set; }
        public string MenuItemNameCn { get; set; }
        public string MenuItemDescription { get; set; }
        public string MenuItemDescriptionCn { get; set; }
        public int? CategoryId { get; set; }
        /// <summary>
        /// 计算时必填
        /// </summary>
        public MenuCalculateTypeEnum MenuCalculateType { get; set; }
        public string Cuisine { get; set; }
        public string Ingredieent { get; set; }
        /// <summary>
        /// 计算时必填
        /// </summary>
        public decimal Price { get; set; }
        public decimal WeBuyPrice { get; set; }
        /// <summary>
        /// 最小人数
        /// </summary>
        public int MinQty { get; set; }
        /// <summary>
        /// 计算时必填
        /// </summary>
        public decimal ChildrenPrice { get; set; }
        public decimal PriceIncrease { get; set; }
        public FoodCategoryEnum Category { get; set; }
        public TrDbRestaurantMenuItem Clone()
        {
            return new TrDbRestaurantMenuItem
            {
                MenuItemName = this.MenuItemName,
                MenuItemNameCn = this.MenuItemNameCn,
                MenuItemDescription = this.MenuItemDescription,
                MenuItemDescriptionCn = this.MenuItemDescriptionCn,
                Cuisine = this.Cuisine,
                Ingredieent = this.Ingredieent,
                Category = this.Category,
                CategoryId = this.CategoryId,
                Id=this.Id
            };
        }
    }
}