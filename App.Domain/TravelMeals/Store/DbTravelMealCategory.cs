using System.Collections.Generic;

namespace App.Domain.TravelMeals.Store
{
    public class DbTravelMealCategory
    {
        public DbTravelMealCategory()
        {
            MenuItems = new List<DbTravelMealItem>();
        }

        public string Name { get; set; }

        public string DisplayName { get; set; }

        public string Description { get; set; }

        public string WebImageUrl { get; set; }

        public bool? Isactive { get; set; }

        //1=Main, 2=Soup
        public int? CategoryGroupId { get; set; }

        public string DisplayExpression { get; set; }

        public string TranslateName { get; set; }

        public int? SortOrder { get; set; }

        public List<DbTravelMealItem> MenuItems { get; set; }
    }
}