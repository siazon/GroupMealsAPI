﻿using App.Domain;
using App.Domain.Common;
using App.Domain.Common.Auth;
using App.Domain.Common.Stripe;
using App.Domain.Config;
using App.Domain.Holiday;
using App.Domain.TravelMeals;
using App.Domain.TravelMeals.Restaurant;
using App.Domain.TravelMeals.VO;
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
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Stripe;
using Stripe.FinancialConnections;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using static Pipelines.Sockets.Unofficial.SocketConnection;

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
        ICountryServiceHandler _countryServiceHandler;
        private readonly IDbCommonRepository<DbPaymentInfo> _paymentRepository;
        IMemoryCache _memoryCache;

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
        public StripeCheckoutController(ICountryServiceHandler countryServiceHandler,
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
            _countryServiceHandler = countryServiceHandler;
            _logger = logger;
            _shopServiceHandler = shopServiceHandler;
            _appsettingConfig = appsettingConfig.Value;
            _stripeUtil = stripeUtil; _memoryCache = memoryCache;
            _amountCalculaterV1 = amountCalculaterV1;
        }
        [HttpGet]
        public string WebhookTest()
        {
            return "Running";
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
                string productId = _stripeUtil.GetProductId(checkoutParam.PayName, checkoutParam.PayDesc);

                _logger.LogDebug("productId:" + productId);
                string priceId = _stripeUtil.GetPriceId(productId, checkoutParam.Amount, checkoutParam.Payment);
                _logger.LogDebug("priceId:" + priceId);
                string sessionUrl = "";
                var res = _trRestaurantBookingServiceHandler.UpdateBooking(checkoutParam.BillId, productId, priceId);
                if (res.Result)
                {
                    sessionUrl = _stripeUtil.Pay(priceId, checkoutParam.Amount);
                }
                _logger.LogDebug("sessionUrl:" + sessionUrl);
                return sessionUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError("StripeException.Error" + ex.Message);
                return "";
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async  Task<IActionResult> GetWebhook()
        {
            //_stripeServiceHandler.SetupPaymentAction(new DbPaymentInfo(), "", "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");

            return Ok("Connected");
        }
        [HttpGet]
        public async Task<IActionResult> GetCurrencies()
        {
            string json = _stripeServiceHandler.GetCurrenciesDB();
            JObject currencyJson = JObject.Parse(json);
            var res = await _countryServiceHandler.GetStripes();
            List<CurrencyInfo> currencyInfos = new List<CurrencyInfo>();
            foreach (var item in res)
            {
                if (item.Currency == "CHF") continue;
                var curr = currencyJson[item.Currency];
                var symbol = curr["symbol"];
                currencyInfos.Add(new CurrencyInfo()
                {
                    Currency = item.Currency,
                    CurrencySymbol = symbol.ToString(),
                    CurrencyName = currencyJson[item.Currency]["name_cn"].ToString()
                });
            }
            return Ok(currencyInfos);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Webhook()
        {
            List<DbStripeEntity> stripes = new List<DbStripeEntity>();
            try
            {
                stripes = await _countryServiceHandler.GetStripes();
            }
            catch (Exception ex)
            {
                return BadRequest("dbstripes is null");
            }


            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();


            _logger.LogInfo("webhook:" + json.ToString());
            string billId = "";
            string userId = "";
            string bookingIds = "";
            try
            {
                Event stripeEvent = null;
                List<string> webhookKeys = new List<string>();
                foreach (var item in stripes)
                {
                    foreach (var key in item.WebhookKeys)
                    {
                        webhookKeys.Add(key);
                    }
                }
                foreach (var item in webhookKeys)
                {
                    try
                    {
                        stripeEvent = EventUtility.ConstructEvent(json, Request.Headers["Stripe-Signature"], item);
                        break;
                    }
                    catch
                    { }
                }
                if (stripeEvent == null)
                {

                    return BadRequest("stripeEvent is null");
                }
                _logger.LogInfo("webhook:stripeEvent.Type:" + stripeEvent.Type);


                switch (stripeEvent.Type)
                {
                    case Events.ChargeSucceeded://支付成功
                        try
                        {
                            _logger.LogInfo("ChargeSucceeded:" + stripeEvent.Type);
                            var paymentIntent = stripeEvent.Data.Object as Charge;
                            billId = "";

                            paymentIntent.Metadata.TryGetValue("billId", out billId);
                            paymentIntent.Metadata.TryGetValue("userId", out userId);
                           paymentIntent.Metadata.TryGetValue("bookingIds",out bookingIds);

                            _trRestaurantBookingServiceHandler.BookingCharged(billId, bookingIds, paymentIntent.Id, paymentIntent.ReceiptUrl);
                            //_trRestaurantBookingServiceHandler.BookingChargedOld(bookingIds, paymentIntent.Id, paymentIntent.ReceiptUrl);

                        }
                        catch (Exception ex)
                        {
                            _logger.LogInfo("ChargeSucceeded.Error:" + ex.Message);
                            return BadRequest("ChargeSucceeded.Error:" + ex.Message);
                        }
                        break;
                    case Events.PaymentIntentSucceeded:
                        try
                        {
                            _logger.LogInfo("ChargeSucceeded:" + stripeEvent.Type);
                            var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                            paymentIntent.Metadata.TryGetValue("billId", out billId);
                            paymentIntent.Metadata.TryGetValue("userId", out userId);
                            bookingIds = paymentIntent.Metadata["bookingIds"];

                            var _dbpayment = await _paymentRepository.GetOneAsync(a => a.Id == billId);
                            if (_dbpayment != null)
                            {
                                var _dbUser = await _customerServiceHandler.GetCustomer(userId, _dbpayment.ShopId ?? 11);
                                List<DbBooking> bookings = _dbUser.CartInfos.FindAll(a => bookingIds.Contains(a.Id));
                                _dbUser.StripeCustomerId = paymentIntent.CustomerId;
                                await _customerServiceHandler.UpdateAccount(_dbUser, _dbpayment.ShopId ?? 11);
                                await _trRestaurantBookingServiceHandler.PlaceBooking(bookings, _dbpayment.ShopId ?? 11, _dbUser, IntentTypeEnum.PaymentIntent);

                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogInfo("PaymentIntentSucceeded.Error:" + ex.Message);
                            return BadRequest("PaymentIntentSucceeded.Error:" + ex.Message);
                        }
                        break;
                    case Events.ChargeRefunded:
                        _logger.LogInfo("ChargeRefunded:" + stripeEvent.Type);
                        var charge = stripeEvent.Data.Object as Stripe.Charge;

                        var paymentInfo = await _paymentRepository.GetOneAsync(a => a.StripeIntentId == charge.PaymentIntentId);
                        if (paymentInfo != null)
                        {
                            paymentInfo.RefundAmount = charge.AmountRefunded / 100;
                        }
                        else
                            return BadRequest("paymentInfo is null");
                        try
                        {
                            await _paymentRepository.UpsertAsync(paymentInfo);
                        }
                        catch (Exception ex)
                        {
                            return BadRequest(" _paymentRepository.UpsertAsync.erroro");
                        }
                     
                        break;
                    case Events.CustomerCreated:
                        var customer = stripeEvent.Data.Object as Customer;
                        userId = customer.Metadata["userId"];
                        var tmeo = customer.Email;
                        var dbUser = await _customerServiceHandler.GetCustomer(userId, 11);
                        dbUser.StripeCustomerId = customer.Id;
                        await _customerServiceHandler.UpdateAccount(dbUser, 11);
                        break;
                    case Events.SetupIntentSucceeded://下单成功
                        _logger.LogInfo("SetupIntentSucceeded:" + stripeEvent.Type);
                        var setupIntent = stripeEvent.Data.Object as SetupIntent;
                        billId = setupIntent.Metadata["billId"];
                        userId = setupIntent.Metadata["userId"];
                        bookingIds = setupIntent.Metadata["bookingIds"];
                        paymentInfo = await _paymentRepository.GetOneAsync(a => a.Id == billId);
                        if (paymentInfo != null)
                        {
                            paymentInfo.StripePaymentMethodId = setupIntent.PaymentMethodId;
                            paymentInfo.CheckoutTime = DateTime.UtcNow;
                        }
                        var dbpayment = await _paymentRepository.UpsertAsync(paymentInfo);
                        if (dbpayment != null)
                        {
                            dbUser = await _customerServiceHandler.GetCustomer(userId, paymentInfo.ShopId ?? 11);
                            List<DbBooking> bookings = dbUser.CartInfos.FindAll(a => a.PaymentId == dbpayment.Id && bookingIds.Contains(a.Id));
                            dbUser.StripeCustomerId = setupIntent.CustomerId;
                            await _customerServiceHandler.UpdateAccount(dbUser, paymentInfo.ShopId ?? 11);
                            await _trRestaurantBookingServiceHandler.PlaceBooking(bookings, paymentInfo.ShopId ?? 11, dbUser, IntentTypeEnum.SetupIntent);
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
                _logger.LogError("StripeException.Error" + e.Message);
                return BadRequest("webhook.error"+ e.Message);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="bill"></param>
        /// <returns></returns>
        [HttpPost]
        [ServiceFilter(typeof(AuthActionFilter))]
        public async Task<IActionResult> CreateSetupIntent([FromBody] PayIntentParam bill)
        {
            Guard.NotNull(bill.CustomerId);
            try
            {
                var authHeader = Request.Headers["Wauthtoken"];
                var user = new TokenEncryptorHelper().Decrypt<DbToken>(authHeader);
                var stripeKeys = await _countryServiceHandler.GetStripes();
                var stripe = stripeKeys.FirstOrDefault(a => a.Currency == bill.Currency);
                SetupIntent setupIntent = _stripeServiceHandler.CreateSetupPayIntent(bill, "", user, stripe.StripeKey);
                return Json(new { clientSecret = setupIntent.ClientSecret });
            }
            catch (Exception ex)
            {
                _logger.LogError("StripeException.Error" + ex.Message);
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
        public ActionResult SetupPayAction([FromBody] string billId)
        {
            var authHeader = Request.Headers["Wauthtoken"];
            var user = new TokenEncryptorHelper().Decrypt<DbToken>(authHeader);
            _trRestaurantBookingServiceHandler.SetupPaymentAction(billId, user.UserId);

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
                TrDbRestaurantBooking gpBooking = await _trRestaurantBookingServiceHandler.GetBookingOld(bill.BillId);
                if (gpBooking == null)
                {
                    return BadRequest("Can't find the booking by Id");
                }

                var paymentinfo = await _paymentRepository.GetOneAsync(a => a.Id == gpBooking.Details[0].PaymentId);
                var stripeKeys = await _countryServiceHandler.GetStripes();
                var stripe = stripeKeys.FirstOrDefault(a => a.Currency == bill.Currency);
                PaymentIntent paymentIntent = _stripeServiceHandler.CreatePayIntent(paymentinfo, bill.BillId, paymentinfo.Creater, stripe.StripeKey);

                paymentinfo.StripeIntentId = paymentIntent.Id;
                paymentinfo.StripeClientSecretKey = paymentIntent.ClientSecret;
                paymentinfo.SetupPay = false;
                await _paymentRepository.UpsertAsync(paymentinfo);

                gpBooking.PaymentInfos[0].StripeIntentId = paymentIntent.Id;
                gpBooking.PaymentInfos[0].SetupPay = false;
                gpBooking.PaymentInfos[0].StripeClientSecretKey = paymentIntent.ClientSecret;
                var res = await _trRestaurantBookingServiceHandler.UpdateBookingOld(gpBooking);


                return Json(new { clientSecret = paymentIntent.ClientSecret, paymentIntentId = paymentIntent.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError("StripeException.Error" + ex.Message);
                return BadRequest(ex.Message);
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bill"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> RefundPay([FromBody] PayIntentParam bill)
        {
            return await ExecuteAsync(11, false, async () => await _stripeServiceHandler.Refund(bill));
        }


    }

}
