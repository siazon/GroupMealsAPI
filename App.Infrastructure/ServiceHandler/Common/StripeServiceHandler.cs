using App.Domain;
using App.Domain.Common.Auth;
using App.Domain.Common.Shop;
using App.Domain.Common.Stripe;
using App.Domain.Holiday;
using App.Domain.TravelMeals;
using App.Infrastructure.Repository;
using App.Infrastructure.Utility.Common;
using App.Infrastructure.Validation;
using Microsoft.Azure.Cosmos;
using Stripe;
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
        SetupIntent CreateSetupPayIntent(PayIntentParam bill, DbToken user);
        void SetupPaymentAction(DbPaymentInfo paymentInfo, string userId);
    }
    public class StripeServiceHandler : IStripeServiceHandler
    {

        private readonly IDbCommonRepository<StripeBase> _stripeBaseRepository;
        private readonly IDbCommonRepository<DbPaymentInfo> _dbPaymentInfoRepository;
        ILogManager _logger;
        public StripeServiceHandler(IDbCommonRepository<StripeBase> stripeBaseRepository, IDbCommonRepository<DbPaymentInfo> dbPaymentInfoRepository, ILogManager logger)
        {
            _stripeBaseRepository = stripeBaseRepository;
            _dbPaymentInfoRepository = dbPaymentInfoRepository;
            _logger = logger;
        }
        public async Task<StripeBase> GetBooking(string id)
        {
            var Booking = await _stripeBaseRepository.GetOneAsync(r => r.Id == id);
            return Booking;
        }


        public SetupIntent CreateSetupPayIntent(PayIntentParam bill, DbToken user)
        {

            Dictionary<string, string> meta = new Dictionary<string, string>();
            meta["billId"] = bill.BillId;
            meta["customerId"] = bill.CustomerId;
            meta["userId"] = user.UserId;
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
            if (setupIntent == null || string.IsNullOrWhiteSpace(setupIntent?.CustomerId))
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
                     Description=user.UserId,
                    Metadata= meta
                });
            }
            return customer;
        }
        public void SetupPaymentAction(DbPaymentInfo paymentInfo, string userId) {

            try
            {
                Dictionary<string, string> meta = new Dictionary<string, string>
                {
                    { "billId", paymentInfo.Id}
                };

                meta["userId"] = userId;
                var options = new PaymentIntentCreateOptions
                {
                    Amount = Convert.ToInt64(paymentInfo.Amount),
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
    }
}
