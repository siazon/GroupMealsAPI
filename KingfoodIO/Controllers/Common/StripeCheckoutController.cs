using App.Domain.Common.Stripe;
using App.Domain.Config;
using App.Domain.Holiday;
using App.Domain.TravelMeals;
using App.Domain.TravelMeals.Restaurant;
using App.Infrastructure.ServiceHandler.Tour;
using App.Infrastructure.ServiceHandler.TravelMeals;
using App.Infrastructure.Utility.Common;
using Azure.Core;
using KingfoodIO.Application.Filter;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Stripe;
using Stripe.FinancialConnections;
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
        private ITourServiceHandler _tourServiceHandler;
        private ITourBookingServiceHandler _tourBookingServiceHandler;
        ILogManager _logger;
        private readonly AppSettingConfig _appsettingConfig;
        private string secret = "";
        public StripeCheckoutController(
          IOptions<CacheSettingConfig> cachesettingConfig, IOptions<AppSettingConfig> appsettingConfig, IRedisCache redisCache, ITourBookingServiceHandler tourBookingServiceHandler, ITourServiceHandler tourServiceHandler, ITrRestaurantBookingServiceHandler restaurantBookingServiceHandler, ILogManager logger) : base(cachesettingConfig, redisCache, logger)
        {
            _trRestaurantBookingServiceHandler = restaurantBookingServiceHandler;
            _tourServiceHandler = tourServiceHandler; 
            _tourBookingServiceHandler = tourBookingServiceHandler;
            _logger = logger;
            _appsettingConfig = appsettingConfig.Value;
            secret = _appsettingConfig.StripeWebhookKey;
        }

        [HttpPost]
        public string Create([FromBody] CheckoutParam checkoutParam)
        {

            try
            {
                checkoutParam.Amount *= 100;
                _logger.LogDebug("Create");
                Console.WriteLine("Create");
                string productId = StripeUtil.GetProductId(checkoutParam.PayName, checkoutParam.PayDesc);

                _logger.LogDebug("productId:" + productId);
                Console.WriteLine("productId:" + productId);
                string priceId = StripeUtil.GetPriceId(productId, checkoutParam.Amount, checkoutParam.Payment);
                _logger.LogDebug("priceId:" + priceId);
                Console.WriteLine("priceId:" + priceId);
                string sessionUrl = "";
                var res = _trRestaurantBookingServiceHandler.UpdateBooking(0, checkoutParam.BillId, productId, priceId);
                if (res.Result)
                {
                    sessionUrl = StripeUtil.Pay(priceId, checkoutParam.Amount);
                }
                _logger.LogDebug("sessionUrl:" + sessionUrl);
                Console.WriteLine("sessionUrl:" + sessionUrl);
                return sessionUrl;
            }
            catch (Exception ex)
            {
                _logger.LogInfo("StripeException.Error" + ex.Message);
                return "";
            }
        }


        [HttpPost]
        public async Task<IActionResult> Webhook()
        {

            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

            _logger.LogInfo("webhook:" + json.ToString());
            Console.WriteLine("CXS WebHook:" + json);
            try
            {
                var stripeEvent = EventUtility.ConstructEvent(json, Request.Headers["Stripe-Signature"], secret);

                _logger.LogInfo("webhook:stripeEvent.Type" + stripeEvent.Type);
                switch (stripeEvent.Type)
                {
                    case Events.ChargeSucceeded:
                        var paymentIntent = stripeEvent.Data.Object as Charge;
                        string bookingId = paymentIntent.Metadata["bookingId"];
                        string billType = paymentIntent.Metadata["billType"];
                        if (billType == "TOUR")
                        {
                            _tourBookingServiceHandler.BookingPaid(bookingId, paymentIntent.CustomerId, paymentIntent.Id, paymentIntent.PaymentMethod, paymentIntent.ReceiptUrl);
                        }
                        else
                        {
                            _trRestaurantBookingServiceHandler.BookingPaid(bookingId, paymentIntent.CustomerId, paymentIntent.PaymentMethod, paymentIntent.ReceiptUrl);
                        }
                        break;
                    case Events.CheckoutSessionCompleted:
                        {
                            _logger.LogInfo("webhook:CheckoutSessionCompleted");
                            var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
                            _logger.LogInfo("webhook:CheckoutSessionCompleted" + session.Id);
                            Task.Run(() =>
                            {
                                _trRestaurantBookingServiceHandler.BookingPaid(session);
                            });
                        }
                        break;
                    case Events.ChargeRefunded:
                        {
                            var charge = stripeEvent.Data.Object as Stripe.Charge;
                            _tourBookingServiceHandler.BookingRefund(charge.Id);
                        }
                        break;
                    case Events.SetupIntentSucceeded:
                        {
                            var setupIntent = stripeEvent.Data.Object as SetupIntent;
                            bookingId = setupIntent.Metadata["bookingId"];
                            _trRestaurantBookingServiceHandler.BookingPaid(bookingId, setupIntent.CustomerId, setupIntent.PaymentMethodId);
                        }
                        break;
                }

                _logger.LogInfo("return OK");
                return Ok();
            }
            catch (StripeException e)
            {
                _logger.LogInfo("StripeException.Error" + e.Message);
                _logger.LogInfo("StripeException.Error" + e);
                return BadRequest();
            }

        }
        [HttpPost]
        public ActionResult CreateSetupIntent([FromBody] string bookingId)
        {
            try
            {
                Dictionary<string, string> meta = new Dictionary<string, string>
            {
                { "bookingId", bookingId}
            };
                var customer = new CustomerService().Create(new CustomerCreateOptions { });

                var options = new SetupIntentCreateOptions
                {
                    //Usage = "on_session",
                    Customer = customer.Id,
                    PaymentMethodTypes = new List<string> { "bancontact", "card", "ideal" },
                    Metadata = meta
                };

                var service = new SetupIntentService();
                var paymentIntent = service.Create(options);
                _trRestaurantBookingServiceHandler.UpdateStripeClientKey(bookingId, paymentIntent.ClientSecret);
                return Json(new { clientSecret = paymentIntent.ClientSecret });
            }
            catch (Exception ex)
            {
                _logger.LogInfo("StripeException.Error" + ex.Message);
                return BadRequest(ex.Message);
            }
        }
        [HttpPost]
        public async Task<ActionResult> SetupPayAction([FromBody] string bookingId)
        {
            try
            {
                Dictionary<string, string> meta = new Dictionary<string, string>
                {
                    { "bookingId", bookingId}
                };
                var booking = await _trRestaurantBookingServiceHandler.GetBooking(11, bookingId);
                var service = new PaymentIntentService();
                var options = new PaymentIntentCreateOptions
                {
                    Amount = await CalculateOrderAmount(bookingId),
                    Currency = "eur",
                    AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                    {
                        Enabled = true,
                    },
                    Customer = booking.StripeCustomerId,
                    PaymentMethod = booking.StripePaymentId,
                    Confirm = true,
                    OffSession = true,
                    ReturnUrl = "https://www.qq.com",
                    Metadata = meta
                };
                service.Create(options);

                var setupService = new SetupIntentService();
                var temp = setupService.Get(booking.StripeClientSecretKey);
                setupService.Cancel(temp.Id);


            }
            catch (StripeException e)
            {
                switch (e.StripeError.Type)
                {
                    case "card_error":
                        // Error code will be authentication_required if authentication is needed
                        Console.WriteLine("Error code: " + e.StripeError.Code);
                        var paymentIntentId = e.StripeError.PaymentIntent.Id;
                        var service = new PaymentIntentService();
                        var paymentIntent = service.Get(paymentIntentId);

                        Console.WriteLine(paymentIntent.Id);
                        break;
                    default:
                        break;
                }
            }
            return Json(new { msg = "OK" });
        }
        [HttpPost]
        public async Task<ActionResult> CreatePayIntent([FromBody] PayIntentParam bill, int shopId)
        {
            try
            {
                Dictionary<string, string> meta = new Dictionary<string, string>
                {
                    { "bookingId", bill.BillId}
                };
                TourBooking booking = null;
               long Amount = 0;
                if (bill.BillType == "TOUR")
                {
                    meta["billType"] = "TOUR";
                     booking = await _tourBookingServiceHandler.GetTourBooking(bill.BillId);
                    Amount = await CalculateTourOrderAmount(booking);
                }
                else
                {
                    meta["billType"] = "GROUPMEALS";
                    Amount = await CalculateOrderAmount(bill.BillId);
                }
                if (booking!=null&&!string.IsNullOrWhiteSpace(booking.StripePaymentId))//upload exist payment
                {
                    var service = new PaymentIntentService(); ;
                    var paymentIntent = service.Get(booking.StripePaymentId);
                    if (paymentIntent != null)
                    {
                        var temo = paymentIntent.Status;
                    }
                    var payment = updatePaymentIntent(booking.StripePaymentId, Amount, meta);
                    return Json(new { clientSecret = payment.ClientSecret, paymentIntentId=payment.Id });
                }
                else// create a new payment
                {
                    var paymentIntentService = new PaymentIntentService();
                    var paymentIntent = paymentIntentService.Create(new PaymentIntentCreateOptions
                    {
                        Amount = Amount,
                        Currency = "eur",
                        AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                        {
                            Enabled = true,
                        },
                        Metadata = meta
                    });
                    _tourBookingServiceHandler.BindingPayInfoToTourBooking(bill.BillId, paymentIntent.Id,paymentIntent.ClientSecret);
                    return Json(new { clientSecret = paymentIntent.ClientSecret, paymentIntentId=paymentIntent.Id });
                }
            }
            catch (Exception ex)
            {
                _logger.LogInfo("StripeException.Error" + ex.Message);
                return BadRequest(ex.Message);
            }

        }
        private PaymentIntent updatePaymentIntent(string paymentId, long Amount, Dictionary<string, string> meta)
        {


            var options = new PaymentIntentUpdateOptions
            {
                Amount = Amount,
                Currency = "eur",
                Metadata = meta
            };
            var service = new PaymentIntentService();
            var payment = service.Update(paymentId, options);
            return payment;
        }

        [HttpPost]
        public async Task<ActionResult> RefundPay([FromBody] PayIntentParam bill, int shopId)
        {
            try
            {
                Dictionary<string, string> meta = new Dictionary<string, string>
                {
                    { "bookingId", bill.BillId}
                };
                string chargeId = "";
                TourBooking tourBooking=null;
                if (bill.BillType == "TOUR")
                {
                     tourBooking = await _tourBookingServiceHandler.GetTourBooking(bill.BillId);
                    if ((DateTime.Now - DateTime.Parse(tourBooking.SelectDate)).TotalHours < 24)
                        chargeId = tourBooking.StripeChargeId;
                    else
                    {
                        return Json(new { msg = "Invalid" });
                    }
                }
                else
                {
                    TrDbRestaurantBooking booking = await _trRestaurantBookingServiceHandler.GetBooking(13, bill.BillId);
                    chargeId = booking.StripeChargeId;
                }
                var options = new RefundCreateOptions
                {
                    Charge = chargeId,
                };
                var service = new RefundService();
                var temp = service.Create(options);
                if (bill.BillType == "TOUR")
                    _tourBookingServiceHandler.EmailCustomerForRefund(tourBooking);
                return Json(new { clientSecret = temp.ChargeId });
            }
            catch (Exception ex)
            {
                _logger.LogInfo("StripeException.Error" + ex.Message);
                return BadRequest(ex.Message);
            }
        }

        private async Task<long> CalculateTourOrderAmount(TourBooking booking)
        {
            // Calculate the order total on the server to prevent
            // people from directly manipulating the amount on the client
            decimal amount = 0;
            if (booking != null)
            {
                amount += (booking.NumberOfPeople ?? 0) * (booking.Tour.Price ?? 0) + (booking.NumberOfAgedOrStudent ?? 0) * (booking.Tour.ConcessionPrice ?? 0) + (booking.NumberOfChild ?? 0) * (booking.Tour.ChildPrice ?? 0);
            }
            return (long)Math.Round(amount, 2) * 100;
        }
        private async Task<long> CalculateOrderAmount(string billId)
        {
            // Calculate the order total on the server to prevent
            // people from directly manipulating the amount on the client
            TrDbRestaurantBooking booking = await _trRestaurantBookingServiceHandler.GetBooking(11, billId);
            decimal amount = 0;
            if (booking != null)
            {
                foreach (var item in booking.Courses)
                {
                    amount += item.Price * item.qty;
                }
            }
            return (long)Math.Round(amount, 2) * 100;
        }
    }

}
