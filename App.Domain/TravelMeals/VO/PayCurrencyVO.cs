using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Domain.TravelMeals.VO
{
    public class PayCurrencyVO
    {
        public string PayCurrency { get; set; }
        public int IntentType { get; set; }
        public List<string> BookingIds { get; set; }
    }
}
