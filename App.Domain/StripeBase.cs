using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Domain
{
    public class StripeBase:DbEntity
    {
        public string StripeProductId { get; set; }
        public string StripePriceId { get; set; }
        public string StripeChargeId { get; set; }
        public string StripePaymentId { get; set; }
        public string StripeCustomerId { get; set; }
        public string StripeReceiptUrl { get; set; }
        public bool StripeSetupIntent { get; set; }
        public string StripeClientSecretKey { get; set; }
        public bool Paid { get; set; }
        public bool SetupPay { get; set; }
    }
}
