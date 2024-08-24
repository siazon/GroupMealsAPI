using App.Domain;
using App.Domain.Common;
using App.Domain.Common.Auth;
using App.Domain.Common.Stripe;
using App.Domain.Config;
using App.Domain.Holiday;
using App.Domain.TravelMeals;
using App.Domain.TravelMeals.Restaurant;
using App.Infrastructure.Repository;
using App.Infrastructure.ServiceHandler.Common;
using App.Infrastructure.ServiceHandler.Tour;
using App.Infrastructure.ServiceHandler.TravelMeals;
using App.Infrastructure.Utility.Common;
using App.Infrastructure.Validation;
using Azure.Core;
using KingfoodIO.Application.Filter;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Caching.Memory;
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
    /// <summary>
    /// 
    /// </summary>
    [Route("api/[controller]/[action]")]
    public class StripeCheckoutController : BaseController
    {
        private ITrRestaurantBookingServiceHandler _trRestaurantBookingServiceHandler;
        private ITourServiceHandler _tourServiceHandler;
        private ITourBookingServiceHandler _tourBookingServiceHandler;
        private IStripeServiceHandler _stripeServiceHandler;
        ILogManager _logger;
        private readonly AppSettingConfig _appsettingConfig;
        IStripeUtil _stripeUtil;
        private readonly IShopServiceHandler _shopServiceHandler;
        IAmountCalculaterUtil _amountCalculaterV1;
        ICustomerServiceHandler _customerServiceHandler;
        private readonly IDbCommonRepository<DbPaymentInfo> _paymentRepository;
        IMemoryCache _memoryCache;
        private string secret = "";

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cachesettingConfig"></param>
        /// <param name="appsettingConfig"></param>
        /// <param name="memoryCache"></param>
        /// <param name="redisCache"></param>
        /// <param name="stripeUtil"></param>
        /// <param name="customerServiceHandler"></param>
        /// <param name="amountCalculaterV1"></param>
        /// <param name="tourBookingServiceHandler"></param>
        /// <param name="tourServiceHandler"></param>
        /// <param name="stripeServiceHandler"></param>
        /// <param name="shopServiceHandler"></param>
        /// <param name="restaurantBookingServiceHandler"></param>
        /// <param name="logger"></param>
        public StripeCheckoutController(
          IOptions<CacheSettingConfig> cachesettingConfig, IOptions<AppSettingConfig> appsettingConfig, IMemoryCache memoryCache, IRedisCache redisCache, IStripeUtil stripeUtil,
          IDbCommonRepository<DbPaymentInfo> paymentRepository,
        ICustomerServiceHandler customerServiceHandler,
        IAmountCalculaterUtil amountCalculaterV1,
        ITourBookingServiceHandler tourBookingServiceHandler, ITourServiceHandler tourServiceHandler, IStripeServiceHandler stripeServiceHandler, IShopServiceHandler shopServiceHandler,
          ITrRestaurantBookingServiceHandler restaurantBookingServiceHandler, ILogManager logger) : base(cachesettingConfig, memoryCache, redisCache, logger)
        {
            _trRestaurantBookingServiceHandler = restaurantBookingServiceHandler;
            _tourServiceHandler = tourServiceHandler;
            _stripeServiceHandler = stripeServiceHandler;
            _tourBookingServiceHandler = tourBookingServiceHandler;
            _customerServiceHandler = customerServiceHandler;
            _paymentRepository = paymentRepository;
            _logger = logger;
            _shopServiceHandler = shopServiceHandler;
            _appsettingConfig = appsettingConfig.Value;
            secret = _appsettingConfig.StripeWebhookKey;
            _stripeUtil = stripeUtil; _memoryCache = memoryCache;
            _amountCalculaterV1 = amountCalculaterV1;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="checkoutParam"></param>
        /// <returns></returns>
        [HttpPost]
        public string Create([FromBody] CheckoutParam checkoutParam)
        {

            try
            {
                checkoutParam.Amount *= 100;
                _logger.LogDebug("Create");
                Console.WriteLine("Create");
                string productId = _stripeUtil.GetProductId(checkoutParam.PayName, checkoutParam.PayDesc);

                _logger.LogDebug("productId:" + productId);
                Console.WriteLine("productId:" + productId);
                string priceId = _stripeUtil.GetPriceId(productId, checkoutParam.Amount, checkoutParam.Payment);
                _logger.LogDebug("priceId:" + priceId);
                Console.WriteLine("priceId:" + priceId);
                string sessionUrl = "";
                var res = _trRestaurantBookingServiceHandler.UpdateBooking(checkoutParam.BillId, productId, priceId);
                if (res.Result)
                {
                    sessionUrl = _stripeUtil.Pay(priceId, checkoutParam.Amount);
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

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> GetWebhook()
        {
            return Ok();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Webhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            _logger.LogInfo("webhook:" + json.ToString());
            Console.WriteLine("CXS WebHook:" + json);
            try
            {
                var stripeEvent = EventUtility.ConstructEvent(json, Request.Headers["Stripe-Signature"], secret);

                _logger.LogInfo("webhook:stripeEvent.Type:" + stripeEvent.Type);
                switch (stripeEvent.Type)
                {
                    case Events.ChargeSucceeded:
                        try
                        {
                            _logger.LogInfo("ChargeSucceeded:" + stripeEvent.Type);
                            var paymentIntent = stripeEvent.Data.Object as Charge;
                            string billId = paymentIntent.Metadata["billId"];
                            var paymentInfos = await _paymentRepository.GetOneAsync(a => a.Id == billId);
                            if (paymentInfos != null)
                            {
                                paymentInfos.StripeChargeId = paymentIntent.Id;
                                paymentInfos.StripeReceiptUrl = paymentIntent.ReceiptUrl;
                                paymentInfos.PayTime = DateTime.UtcNow;
                                paymentInfos.Paid = true;
                            }
                            await _paymentRepository.UpsertAsync(paymentInfos);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogInfo("ChargeSucceeded&PaymentIntentSucceeded.Error:" + ex.Message);
                        }
                        break;
                    case Events.PaymentIntentSucceeded:
                        try
                        {
                            _logger.LogInfo("ChargeSucceeded:" + stripeEvent.Type);
                            var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                            string billId = paymentIntent.Metadata["billId"];

                            _trRestaurantBookingServiceHandler.BookingPaid(billId, paymentIntent.CustomerId, paymentIntent.Id, paymentIntent.PaymentMethodId);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogInfo("ChargeSucceeded&PaymentIntentSucceeded.Error:" + ex.Message);
                        }
                        break;
                    case Events.CheckoutSessionCompleted:
                        {
                            _logger.LogInfo("CheckoutSessionCompleted:" + stripeEvent.Type);
                            _logger.LogInfo("webhook:CheckoutSessionCompleted");
                            var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
                            _logger.LogInfo("webhook:CheckoutSessionCompleted" + session.Id);

                        }
                        break;
                    case Events.ChargeRefunded:
                        _logger.LogInfo("ChargeRefunded:" + stripeEvent.Type);
                        var charge = stripeEvent.Data.Object as Stripe.Charge;

                        var paymentInfo = await _paymentRepository.GetOneAsync(a => a.Id == "");
                        if (paymentInfo != null)
                        {
                            paymentInfo.RefundAmount = charge.AmountRefunded/100; 
                        }
                        await _paymentRepository.UpsertAsync(paymentInfo);
                        break;
                    case Events.SetupIntentSucceeded:
                        {
                            _logger.LogInfo("SetupIntentSucceeded:" + stripeEvent.Type);
                            var setupIntent = stripeEvent.Data.Object as SetupIntent;
                            string billId = setupIntent.Metadata["billId"];

                            paymentInfo = await _paymentRepository.GetOneAsync(a => a.Id == billId);
                            if (paymentInfo != null)
                            {
                                paymentInfo.StripePaymentMethodId = setupIntent.PaymentMethodId;
                            }
                            await _paymentRepository.UpsertAsync(paymentInfo);
                        }
                        break;
                    default:
                        {
                            _logger.LogInfo("default:" + stripeEvent.Type);
                            break;
                        }
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
        /// <summary>
        /// 
        /// </summary>
        /// <param name="bill"></param>
        /// <returns></returns>
        [HttpPost]
        [ServiceFilter(typeof(AuthActionFilter))]
        public ActionResult CreateSetupIntent([FromBody] PayIntentParam bill)
        {
            Guard.NotNull(bill.CustomerId);
            try
            {
                var authHeader = Request.Headers["Wauthtoken"];
                var user = new TokenEncryptorHelper().Decrypt<DbToken>(authHeader);
                SetupIntent setupIntent = _stripeServiceHandler.CreateSetupPayIntent(bill, user);
                return Json(new { clientSecret = setupIntent.ClientSecret });
            }
            catch (Exception ex)
            {
                _logger.LogInfo("StripeException.Error" + ex.Message);
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="billId"></param>
        /// <returns></returns>
        [HttpPost]
        [ServiceFilter(typeof(AuthActionFilter))]
        public async Task<ActionResult> SetupPayAction([FromBody] string billId)
        {
            try
            {
                Dictionary<string, string> meta = new Dictionary<string, string>
                {
                    { "billId", billId}
                };
                var authHeader = Request.Headers["Wauthtoken"];
                var user = new TokenEncryptorHelper().Decrypt<DbToken>(authHeader);
                var customer = await _customerServiceHandler.GetCustomer(user.UserId, user.ShopId ?? 11);
                var paymentInfo = await _paymentRepository.GetOneAsync(a => a.Id == customer.CartInfos[0].AmountInfos[0].PaymentId);
                var options = new PaymentIntentCreateOptions
                {
                    Amount = Convert.ToInt64(paymentInfo.PaidAmount),
                    Currency = paymentInfo.Currency,
                    AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                    {
                        Enabled = true,
                    },
                    Customer = paymentInfo.StripeCustomerId,
                    PaymentMethod = paymentInfo.StripePaymentMethodId,
                    Confirm = true,
                    OffSession = true,
                    ReturnUrl = "https://www.groupmeals.com",
                    Metadata = meta
                };
                var service = new PaymentIntentService();
                service.Create(options);
            }
            catch (StripeException e)
            {
                _logger.LogError("Error code: " + e.Message);
                switch (e.StripeError.Type)
                {
                    case "card_error":
                        // Error code will be authentication_required if authentication is needed
                        _logger.LogError("Error code: " + e.StripeError.Code + " : " + e.Message);
                        var paymentIntentId = e.StripeError.PaymentIntent.Id;
                        var service = new PaymentIntentService();
                        var paymentIntent = service.Get(paymentIntentId);

                        _logger.LogError(paymentIntent.Id);
                        break;
                    default:
                        break;
                }
            }
            return Json(new { msg = "OK" });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bill"></param>
        /// <param name="shopId"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult> CreatePayIntent([FromBody] PayIntentParam bill, int shopId)
        {
            try
            {
                Dictionary<string, string> meta = new Dictionary<string, string>
                {
                    { "billId", bill.BillId}
                };
                TrDbRestaurantBooking booking = null;
                TrDbRestaurantBooking gpBooking = null;
                long Amount = 0;


                gpBooking = await _trRestaurantBookingServiceHandler.GetBooking(bill.BillId);
                if (gpBooking == null)
                {
                    return BadRequest("Can't find the booking by Id");
                }
                booking = gpBooking;
                var shop = await _shopServiceHandler.GetShopInfo(shopId);
                if (bill.SetupPay == 1)
                    Amount = 100;
                else
                    Amount = await _amountCalculaterV1.CalculateOrderPaidAmount(gpBooking.Details, gpBooking.PayCurrency, gpBooking.ShopId ?? 11);//  CalculateOrderAmount(gpBooking, shop.ExchangeRate);
                string currency = "eur";
                if (gpBooking.PayCurrency == "UK")
                    currency = "gbp";
                if (booking != null && !string.IsNullOrWhiteSpace(booking.PaymentInfos[0].StripePaymentMethodId))//upload exist payment
                {
                    var service = new PaymentIntentService();
                    var existpaymentIntent = service.Get(booking.PaymentInfos[0].StripePaymentMethodId);
                    if (existpaymentIntent != null && existpaymentIntent.Status == "requires_payment_method")
                    {
                        var payment = UpdatePaymentIntent(booking.PaymentInfos[0].StripePaymentMethodId, Amount, meta);
                        _logger.LogInfo("UpdatePaymentIntent:" + booking.ToString() + bill.ToString());
                        return Json(new { clientSecret = payment.ClientSecret, paymentIntentId = payment.Id });
                    }
                }
                var options = new PaymentIntentCreateOptions
                {
                    Amount = Amount,
                    Currency = currency,
                    //PaymentMethodTypes = new List<string> { "card","alipay", "wechat_pay" },
                    AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                    {
                        Enabled = true,
                    },
                    Metadata = meta
                };
                var paymentIntentService = new PaymentIntentService();
                var paymentIntent = paymentIntentService.Create(options);
                _logger.LogInfo("AddPaymentIntent:" + booking.ToString() + bill.ToString());
                _trRestaurantBookingServiceHandler.BindingPayInfoToTourBooking(gpBooking, paymentIntent.Id, paymentIntent.ClientSecret, bill.SetupPay == 1);
                return Json(new { clientSecret = paymentIntent.ClientSecret, paymentIntentId = paymentIntent.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError("StripeException.Error" + ex.Message);
                return BadRequest(ex.Message);
            }

        }
        private PaymentIntent UpdatePaymentIntent(string paymentId, long Amount, Dictionary<string, string> meta)
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bill"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult RefundPay([FromBody] PayIntentParam bill)
        {

            try
            {
                Dictionary<string, string> meta = new Dictionary<string, string>
                {
                    { "billId", bill.BillId}
                };
                string chargeId = "ch_3Pr72sAwWylbYgqy2FyCO45V";
                var options = new RefundCreateOptions
                {
                    Charge = chargeId,
                    Amount = 100
                };
                var service = new RefundService();
                var temp = service.Create(options);
                return Json(new { msg = "OK", billId = temp.ChargeId });
            }
            catch (Exception ex)
            {
                _logger.LogInfo("StripeException.Error" + ex.Message);
                return BadRequest(ex.Message);
            }
        }


    }

}
