using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace App.Domain.TravelMeals.Trip
{
    public class DbTrip : DbEntity
    {
        public string Name { get; set; }
        public string Description { get; set; }

        [Column(TypeName = "decimal(18, 2)")] public decimal? Price { get; set; }

        public string Image { get; set; }
        public List<string> ImageList { get; set; }

        public int? Rating { get; set; }

        public int? Tag { get; set; }

        public bool? IsActive { get; set; }
    }
}