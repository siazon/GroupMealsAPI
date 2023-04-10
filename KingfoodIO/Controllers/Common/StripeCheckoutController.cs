using App.Domain.Common.Stripe;
using App.Domain.Config;
using App.Domain.TravelMeals;
using App.Domain.TravelMeals.Restaurant;
using App.Infrastructure.ServiceHandler.TravelMeals;
using App.Infrastructure.Utility.Common;
using KingfoodIO.Application.Filter;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Stripe;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace KingfoodIO.Controllers.Common
{
    [Route("api/[controller]/[action]")]
    public class StripeCheckoutController : BaseController
    {
        private ITrRestaurantBookingServiceHandler _trRestaurantBookingServiceHandler;
        ILogManager _logger;
        public StripeCheckoutController(
          IOptions<CacheSettingConfig> cachesettingConfig, IRedisCache redisCache,   ITrRestaurantBookingServiceHandler restaurantBookingServiceHandler, ILogManager logger) : base(cachesettingConfig, redisCache, logger)
        {
            _trRestaurantBookingServiceHandler = restaurantBookingServiceHandler;
            _logger= logger;
        }
        [HttpPost]
        public string Create([FromBody] CheckoutParam checkoutParam)
        {
            
            checkoutParam.Amount *= 100;
            _logger.LogDebug("Create");
            Console.WriteLine("Create");
            string productId = StripeUtil.GetProductId(checkoutParam.PayName, checkoutParam.PayDesc);

            _logger.LogDebug("productId:"+ productId);
            Console.WriteLine("productId:" + productId);
            string priceId = StripeUtil.GetPriceId(productId, checkoutParam.Amount, checkoutParam.Payment);
            _logger.LogDebug("priceId:" + priceId);
            Console.WriteLine("priceId:" + priceId);
            string sessionUrl = "";
            var res = _trRestaurantBookingServiceHandler.UpdateBooking(0, checkoutParam.BillId, productId, priceId);
            if (res.Result)
            {
                sessionUrl = StripeUtil.Pay(priceId,checkoutParam.Amount);
            }
            _logger.LogDebug("sessionUrl:" + sessionUrl);
            Console.WriteLine("sessionUrl:" + sessionUrl);
            return sessionUrl;
        }

        const string secret = "whsec_317c4779f25ef651247a72a3fdc9d602e020a74896cb322511d97df5e700a87e";
        [HttpPost]
        public async Task<IActionResult> Index()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

            Console.WriteLine("CXS WebHook:" + json);
            try
            {
                var stripeEvent = EventUtility.ConstructEvent(json,Request.Headers["Stripe-Signature"],secret);
                switch (stripeEvent.Type) {
                    case Events.ChargeSucceeded:
                        var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                        break;
                    case Events.CheckoutSessionCompleted:
                        {
                            var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
                            _trRestaurantBookingServiceHandler.BookingPaid(session);

                        }
                        break;
                }
                return Ok();
            }
            catch (StripeException e)
            {
                return BadRequest();
            }

        }
    }
}
