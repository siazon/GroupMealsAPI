using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Domain.TravelMeals.Restaurant
{
    public class DbCountry : DbEntity
    {
        public List<Country> Countries { get; set; }

    }
    public class Country {
        public int SortOrder { get; set; }
        public string Name { get; set; }
        public List<City> Cities { get; set; }
    }
    public class City
    {
        public int SortOrder { get; set; }
        public string Name { get; set; }
    }
}
