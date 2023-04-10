using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Domain.Common.Stripe
{
    public class StripeCheckoutSeesion: DbEntity
    {
        public string BookingId { get; set; }
        public Object Data { get; set; }
    }
}
