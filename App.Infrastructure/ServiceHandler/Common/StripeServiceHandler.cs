using App.Domain;
using App.Domain.Common;
using App.Domain.Common.Auth;
using App.Domain.Common.Shop;
using App.Domain.Common.Stripe;
using App.Domain.Holiday;
using App.Domain.TravelMeals;
using App.Domain.TravelMeals.Restaurant;
using App.Infrastructure.Repository;
using App.Infrastructure.Utility.Common;
using App.Infrastructure.Validation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.FinancialConnections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Infrastructure.ServiceHandler.Common
{
    public interface IStripeServiceHandler
    {
        Task<StripeBase> GetBooking(string id);
        SetupIntent CreateSetupPayIntent(PayIntentParam bill, string bookingIds, DbToken user, string stripeKey);
        PaymentIntent CreatePayIntent(DbPaymentInfo dbPaymentInfo, string bookingIds, string userId, string stripeKey);
        void SetupPaymentAction(DbPaymentInfo paymentInfo, string userId, string stripeKey);
        Task<ResponseModel> Refund(PayIntentParam bill);
        string GetCurrenciesDB();
    }
    public class StripeServiceHandler : IStripeServiceHandler
    {

        private readonly IDbCommonRepository<StripeBase> _stripeBaseRepository;
        private readonly IDbCommonRepository<DbPaymentInfo> _dbPaymentInfoRepository;
        IWebHostEnvironment _environment;
        ILogManager _logger;
        public StripeServiceHandler(IDbCommonRepository<StripeBase> stripeBaseRepository, IDbCommonRepository<DbPaymentInfo> dbPaymentInfoRepository, IWebHostEnvironment webHostEnvironment, ILogManager logger)
        {
            _stripeBaseRepository = stripeBaseRepository;
            _dbPaymentInfoRepository = dbPaymentInfoRepository;
            _logger = logger;
            _environment = webHostEnvironment;
        }
        public async Task<StripeBase> GetBooking(string id)
        {
            var Booking = await _stripeBaseRepository.GetOneAsync(r => r.Id == id);
            return Booking;
        }

        public PaymentIntent CreatePayIntent(DbPaymentInfo dbPaymentInfo, string bookingIds, string userId, string stripeKey)
        {
            StripeConfiguration.ApiKey = stripeKey;
            Dictionary<string, string> meta = new Dictionary<string, string>
                {
                    { "billId", dbPaymentInfo.Id}
                };
            meta["bookingIds"] = bookingIds;
            meta["userId"] = userId;
            meta["intent_type"] = "2";
            var paymentIntentService = new PaymentIntentService();
            PaymentIntent paymentIntent = null;
            if (!string.IsNullOrWhiteSpace(dbPaymentInfo.StripeIntentId))
            {
                try
                {
                    paymentIntent = paymentIntentService.Get(dbPaymentInfo.StripeIntentId);
                    if (paymentIntent.Status == "succeeded") {
                        paymentIntent = null;
                    }
                    else
                    {
                        PaymentIntentUpdateOptions options = new PaymentIntentUpdateOptions
                        {
                            Amount = Convert.ToInt64(dbPaymentInfo.Amount * 100),
                            Currency = dbPaymentInfo.Currency,
                            Metadata = meta
                        };
                        paymentIntent = paymentIntentService.Update(paymentIntent.Id, options);
                        return paymentIntent;
                    }
                    

                }
                catch (Exception ex)
                { }
             
            }
            if (paymentIntent == null || paymentIntent.Status == "succeeded" || string.IsNullOrWhiteSpace(paymentIntent?.CustomerId))
            {
                try
                {
                    var options = new PaymentIntentCreateOptions
                    {
                        Amount = Convert.ToInt64(dbPaymentInfo.Amount * 100),
                        Currency = dbPaymentInfo.Currency,
                        //PaymentMethodTypes = new List<string> { "card", "alipay", "wechat_pay" },
                        AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                        {
                            Enabled = true,
                        },
                        Metadata = meta
                    };

                    paymentIntent = paymentIntentService.Create(options);
                }
                catch (Exception ex)
                {

                }

            }
            return paymentIntent;
        }

        public SetupIntent CreateSetupPayIntent(PayIntentParam bill, string bookingIds, DbToken user, string stripeKey)
        {
            StripeConfiguration.ApiKey = stripeKey;
            Dictionary<string, string> meta = new Dictionary<string, string>();
            meta["billId"] = bill.BillId;
            meta["bookingIds"] = bookingIds;
            meta["customerId"] = bill.CustomerId;
            meta["userId"] = user.UserId;
            meta["intent_type"] = "1";
            var setupIntentService = new SetupIntentService();
            SetupIntent setupIntent = null;
            if (!string.IsNullOrWhiteSpace(bill.PaymentIntentId))
            {
                try
                {
                    setupIntent = setupIntentService.Get(bill.PaymentIntentId);
                }
                catch (Exception ex)
                { }
            }
            if (setupIntent == null || setupIntent.Status == "succeeded" || string.IsNullOrWhiteSpace(setupIntent?.CustomerId))
            {
                string customerId = bill.CustomerId;

                customerId = CreateCustomer(user, customerId)?.Id;
                if (string.IsNullOrEmpty(customerId))
                {
                    return null;
                }
                setupIntent = setupIntentService.Create(new SetupIntentCreateOptions
                {
                    //Usage = "on_session",
                    Customer = customerId,
                    PaymentMethodTypes = new List<string> { "bancontact", "card", "ideal" },
                    Metadata = meta
                });
            }
            return setupIntent;
        }
        private Customer CreateCustomer(DbToken user, string customerId)
        {
            Dictionary<string, string> meta = new Dictionary<string, string>();
            meta["userId"] = user.UserId;
            Customer customer = null;
            if (!string.IsNullOrWhiteSpace(customerId))
            {
                try
                {
                    var customerService = new CustomerService();
                    customer = customerService.Get(customerId);
                }
                catch (Exception ex)
                {
                }
            }
            if (customer == null)
            {
                customer = new CustomerService().Create(new CustomerCreateOptions
                {
                    Name = user.UserName,
                    Email = user.UserEmail,
                    Description = user.UserId,
                    Metadata = meta
                });
            }
            return customer;
        }
        public void SetupPaymentAction(DbPaymentInfo paymentInfo, string userId, string stripeKey)
        {
            StripeConfiguration.ApiKey = stripeKey;
            try
            {
                Dictionary<string, string> meta = new Dictionary<string, string>
                {
                    { "billId", paymentInfo.Id}
                };

                meta["userId"] = userId;
                var options = new PaymentIntentCreateOptions
                {
                    Amount = Convert.ToInt64(paymentInfo.Amount * 100),
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
        }
        public async Task<ResponseModel> Refund(PayIntentParam bill)
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
                return new ResponseModel { code = 200, msg = "ok" };// Json(new { msg = "OK", billId = temp.ChargeId });
            }
            catch (Exception ex)
            {
                _logger.LogInfo("StripeException.Error" + ex.Message);
                return new ResponseModel { code = 501, msg = ex.Message };// BadRequest(ex.Message);
            }
        }
        public string GetCurrenciesDB() {
            string currecyJson = EmailTemplateUtil.ReadJson(this._environment.WebRootPath, "currencydb");
            return currecyJson;

        }
    }
}
