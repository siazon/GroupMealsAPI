using App.Domain;
using App.Domain.Common.Auth;
using App.Domain.Common.Shop;
using App.Domain.Common.Stripe;
using App.Domain.Holiday;
using App.Domain.TravelMeals;
using App.Infrastructure.Repository;
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
    }
    public class StripeServiceHandler : IStripeServiceHandler
    {

        private readonly IDbCommonRepository<StripeBase> _stripeBaseRepository;
        private readonly IDbCommonRepository<DbPaymentInfo> _dbPaymentInfoRepository;
        public StripeServiceHandler(IDbCommonRepository<StripeBase> stripeBaseRepository, IDbCommonRepository<DbPaymentInfo> dbPaymentInfoRepository)
        {
            _stripeBaseRepository = stripeBaseRepository;
            _dbPaymentInfoRepository = dbPaymentInfoRepository;
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
                });
            }
            return customer;
        }
    }
}
