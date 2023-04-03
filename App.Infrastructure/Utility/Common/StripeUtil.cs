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
        public static string Pay(string name, string description, int amount, string payment = "eur")
        {
            StripeConfiguration.ApiKey = "sk_test_51MsNeuEOhoHb4C89kuTDIQd4WTiRiWGXSrFMnJMxsk0ufrGw7VMTsilTZKmVYbYn9zHyW98De7hXcrOwfrbGJXcY00DE8tswlW";

            var optionsProduct = new ProductCreateOptions
            {
                Name = name,
                Description = description,
            };
            var serviceProduct = new ProductService();
            Product product = serviceProduct.Create(optionsProduct);
            Console.Write("Success! Here is your starter subscription product id: {0}\n", product.Id);

            var optionsPrice = new PriceCreateOptions
            {
                UnitAmount = amount,
                Currency = payment,
                Product = product.Id,
            };
            var servicePrice = new PriceService();
            Price price = servicePrice.Create(optionsPrice);

            var options = new SessionCreateOptions
            {
                LineItems = new List<SessionLineItemOptions>
                {
                  new SessionLineItemOptions
                  {
                    // Provide the exact Price ID (for example, pr_1234) of the product you want to sell
                    Price =price.Id,// "price_1MsOI6EOhoHb4C89WQ6OWpsJ",
                    Quantity = 1,
                  },
                },
                Mode = "payment",
                SuccessUrl = "http://127.0.0.1:2712/Orders",
                CancelUrl =  "http://127.0.0.1:2712/ShopBooking",
                AutomaticTax = new SessionAutomaticTaxOptions { Enabled = true },
            };
            var service = new SessionService();
            Session session = service.Create(options);
            return session.Url; 
        }

    }
}
