using App.Domain.Config;
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
    public class StripeCheckoutController : Controller
    {
        [HttpPost]
        public string Create()
        {
            string sessionUrl = StripeUtil.Pay("Lily's cafe 30 Group Meals", "12*25 Adults 8*5 Children", 34000, "usd");
           
            //Response.Headers.Add("Location", sessionUrl);
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
                var stripeEvent = EventUtility.ConstructEvent(
                  json,
                  Request.Headers["Stripe-Signature"],
                  secret
                );

                return Ok();
            }
            catch (StripeException e)
            {
                return BadRequest();
            }

        }
    }
}
