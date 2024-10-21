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
        public IntentTypeEnum IntentType { get; set; }
        public List<string> BookingIds { get; set; }
    }
    public enum IntentTypeEnum { 
        None,
    PaymentIntent,
    SetupIntent
    }
}
