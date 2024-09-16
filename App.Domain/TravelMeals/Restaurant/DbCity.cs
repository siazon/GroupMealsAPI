using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Domain.TravelMeals.Restaurant
{
    public class DbCountry : DbEntity
    {
        public string Name { get; set; }
        public string Code { get; set; }
        public string Currency { get; set; }
        public double VAT { get; set; }
        public double ExchangeRate { get; set; }
        public double ExchangeRateExtra { get; set; }
        public string CurrencySymbol { get; set; }
        public List<City> Cities { get; set; }

    } 
    public class City
    {
        public int SortOrder { get; set; }
        public string Name { get; set; }
        public string TimeZone { get; set; }
    }
}
