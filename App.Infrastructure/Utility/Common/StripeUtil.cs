using Microsoft.AspNetCore.Http;
using Stripe;
using Stripe.Checkout;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Infrastructure.Utility.Common
{
    public class StripeUtil
    {


        const string key = "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";// "sk_test_51MsNeuEOhoHb4C89kuTDIQd4WTiRiWGXSrFMnJMxsk0ufrGw7VMTsilTZKmVYbYn9zHyW98De7hXcrOwfrbGJXcY00DE8tswlW";

        public static string GetProductId(string name, string description)
        {
            StripeConfiguration.ApiKey = key;
            var optionsProduct = new ProductCreateOptions
            {
                Name = name,
                Description = description,
            };
            var serviceProduct = new ProductService();
            Product product = serviceProduct.Create(optionsProduct);
            return product.Id;
        }
        public static string GetPriceId(string productId, long amount, string payment)
        {
            StripeConfiguration.ApiKey = key;
            var optionsPrice = new PriceCreateOptions
            {
                UnitAmount = amount,
                Currency = payment,
                Product = productId,
            };
            var servicePrice = new PriceService();
            Price price = servicePrice.Create(optionsPrice);
            return price.Id;
        }
        public static string GetPriceId(string name, string description, long amount, string payment)
        {
            StripeConfiguration.ApiKey = key;
            string productId = GetProductId(name, description);
            var optionsPrice = new PriceCreateOptions
            {
                UnitAmount = amount,
                Currency = payment,
                Product = productId,
            };
            var servicePrice = new PriceService();
            Price price = servicePrice.Create(optionsPrice);
            return price.Id;
        }
        public static string GetTax()
        {
            StripeConfiguration.ApiKey = key;
            var options = new TaxRateCreateOptions
            {
                DisplayName = "Sales Tax",
                Inclusive = false,
                Percentage = 7.25m,
                Country = "US",
                State = "CA",
                Jurisdiction = "US - CA",
                Description = "CA Sales Tax",
            };
            var service = new TaxRateService();
            TaxRate taxRate = service.Create(options);
            return taxRate.Id;
        }
        public static string Pay(string priceId, decimal amount)
        {

            StripeConfiguration.ApiKey = key;
            Dictionary<string, string> meta = new Dictionary<string, string>
            {
                { "priceId", priceId }
            };
            string successUrl = "", cancelUrl = "";
#if DEBUG
            successUrl = "http://127.0.0.1:2712/ShopBookingComplete?amount=" + amount;
            cancelUrl = "http://127.0.0.1:2712/ShopBooking";
#else
           successUrl = "https://groupmeals.z16.web.core.windows.net/ShopBookingComplete?amount="+ amount;
            cancelUrl = "https://groupmeals.z16.web.core.windows.net/ShopBooking";
#endif

            string TaxId = GetTax();

            var options = new SessionCreateOptions
            {
                LineItems = new List<SessionLineItemOptions>
                {
                  new SessionLineItemOptions
                  {
                    Price =priceId,
                    Quantity = 1,
                   TaxRates= new List<string>{ TaxId, },
                  },
                },
                Mode = "payment",
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
                //AutomaticTax = new SessionAutomaticTaxOptions { Enabled = true },
                Metadata = meta
            };
            var service = new SessionService();
            Session session = service.Create(options);
            return session.Url;
        }
        public static string Pay(string name, string description, int amount, string payment = "eur")
        {
            StripeConfiguration.ApiKey = key;
            string priceId = GetPriceId(name, description, amount, payment);
            var options = new SessionCreateOptions
            {
                LineItems = new List<SessionLineItemOptions>
                {
                  new SessionLineItemOptions
                  {
                    Price =priceId,
                    Quantity = 1,
                  },
                },
                Mode = "payment",
                SuccessUrl = "http://127.0.0.1:2712/ShopBookingComplete",
                CancelUrl = "http://127.0.0.1:2712/ShopBooking",
                AutomaticTax = new SessionAutomaticTaxOptions { Enabled = true },
            };
            var service = new SessionService();
            Session session = service.Create(options);
            return session.Url;
        }

    }
}
