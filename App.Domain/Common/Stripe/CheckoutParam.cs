using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Domain.Common.Stripe
{
    public class CheckoutParam
    {
        public string BillId { get; set; }
        public string PayName { get; set; }
        public string PayDesc { get; set; }
        public int Amount { get; set; }
        public string Payment { get; set; }
    }
}
