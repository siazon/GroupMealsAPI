using App.Domain.Common.Stripe;
using App.Domain.Holiday;
using App.Domain.TravelMeals;
using App.Infrastructure.ServiceHandler.Tour;
using App.Infrastructure.ServiceHandler.TravelMeals;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Infrastructure.Utility.Common
{
    public interface IStripeUtil
    {
        string GetProductId(string name, string productId);
        string GetPriceId(string productId, long amount, string payment);
        string Pay(string priceId, decimal amount);
        void RefundGroupMeals(TrDbRestaurantBooking bill);
    }
    public class StripeUtil : IStripeUtil
    {
        ILogManager _logger;
        public StripeUtil(ILogManager logger)
        {
            _logger = logger;
        }

        //const string key = "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";
        const string key = "sk_test_51MsNeuEOhoHb4C89kuTDIQd4WTiRiWGXSrFMnJMxsk0ufrGw7VMTsilTZKmVYbYn9zHyW98De7hXcrOwfrbGJXcY00DE8tswlW";

        public string GetProductId(string name, string description)
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
        public string GetPriceId(string productId, long amount, string payment)
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
        public string GetPriceId(string name, string description, long amount, string payment)
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
        public string GetTax()
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
        public string Pay(string priceId, decimal amount)
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
        public string Pay(string name, string description, int amount, string payment = "eur")
        {

            string successUrl = "", cancelUrl = "";
#if DEBUG
            successUrl = "http://127.0.0.1:2712/ShopBookingComplete?amount=" + amount;
            cancelUrl = "http://127.0.0.1:2712/ShopBooking";
#else
           successUrl = "https://groupmeals.z16.web.core.windows.net/ShopBookingComplete?amount="+ amount;
            cancelUrl = "https://groupmeals.z16.web.core.windows.net/ShopBooking";
#endif

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
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
                AutomaticTax = new SessionAutomaticTaxOptions { Enabled = true },
            };
            var service = new SessionService();
            Session session = service.Create(options);
            return session.Url;
        }

        public  void RefundGroupMeals(TrDbRestaurantBooking booking)
        {
            try
            {
                string chargeId = booking.PaymentInfos[0].StripeChargeId;
                var options = new RefundCreateOptions
                {
                    Charge = chargeId,
                    Amount = CalculateOrderAmount(booking)
                };
                var service = new RefundService();
                var temp = service.Create(options);
            }
            catch (Exception ex)
            {
                _logger.LogInfo("StripeException.Error" + ex.Message);
            }
        }


        private long CalculateOrderAmount(TrDbRestaurantBooking booking)
        {
            // Calculate the order total on the server to prevent
            // people from directly manipulating the amount on the client
            decimal amount = 0;
            if (booking != null)
            {
                foreach (var course in booking.Details)
                {
                    foreach (var item in course.Courses)
                    {
                        if (item.Qty < 4)
                        {
                            _logger.LogInfo("StripeCheckoutController.CalculateOrderAmount:people Qty too small");
                            throw new Exception("people Qty too small");
                        }
                        if (item.CourseType == 0)
                        {
                            item.Amount = item.Price * item.Qty;
                            amount += item.Amount;
                            continue;
                        }
                        if (item.Qty == 4 || item.Qty == 5)
                        {
                            item.Amount = 10 * item.Price * 0.8m;
                            amount += item.Amount;
                        }
                        else if (item.Qty == 6 || item.Qty == 7)
                        {
                            item.Amount = 10 * item.Price * 0.85m;
                            amount += item.Amount;
                        }
                        else if (item.Qty == 8)
                        {
                            item.Amount = 10 * item.Price * 0.9m;
                            amount += item.Amount;
                        }
                        else if (item.Qty == 9)
                        {
                            item.Amount = 10 * item.Price * 0.95m;
                            amount += item.Amount;
                        }
                        else
                        {
                            item.Amount = item.Price * item.Qty;
                            amount += item.Amount;
                        }
                    }
                }

            }
            return (long)Math.Round(amount, 2) * 100;
        }

    }
}
