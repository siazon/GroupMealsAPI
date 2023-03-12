using System.ComponentModel.DataAnnotations.Schema;

namespace App.Domain.TravelMeals.Store
{
    public class DbTravelMealItem : DbEntity
    {
        public string Name { get; set; }

        public string DisplayName { get; set; }

        public string TranslatedName { get; set; }

        [Column(TypeName = "decimal(18, 2)")] public decimal? Price { get; set; }

        public string Description { get; set; }

        public string ImageUrl { get; set; }

        public string VideoLink { get; set; }

        public string ExtraComment { get; set; }

        //This is for customer to add comment
        public string Comment { get; set; }

        public bool? Isactive { get; set; }

        public int? SortOrder { get; set; }

        //1=starter. 2=main, 3=source, 4=drinks
        public int? FoodCategoryId { get; set; }
    }
}