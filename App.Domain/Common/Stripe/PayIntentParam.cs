using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Domain.Common.Stripe
{
    public class PayIntentParam
    {

        public string BillType { get; set; }
        public string BillId { get; set; }
        public int SetupPay { get; set; }
        public string PaymentIntentId { get; set; }
    }
}
