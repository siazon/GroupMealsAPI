using App.Domain;
using App.Domain.Common.Shop;
using App.Domain.Holiday;
using App.Infrastructure.Repository;
using App.Infrastructure.Validation;
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
        Task<StripeBase> BindingPayInfoToBooking(string bookingId, string PaymentId, string stripeClientSecretKey);
    }
    public class StripeServiceHandler: IStripeServiceHandler
    {

        private readonly IDbCommonRepository<StripeBase> _stripeBaseRepository;
        public StripeServiceHandler(IDbCommonRepository<StripeBase> stripeBaseRepository) {
            _stripeBaseRepository=stripeBaseRepository;
        }
        public async Task<StripeBase> GetBooking(string id)
        {
            var Booking = await _stripeBaseRepository.GetOneAsync(r => r.Id == id);
            return Booking;
        }

        public async Task<StripeBase> BindingPayInfoToBooking(string bookingId, string PaymentId, string stripeClientSecretKey)
        {
            var booking = await _stripeBaseRepository.GetOneAsync(r => r.Id == bookingId);
            Guard.NotNull(booking);
            booking.StripePaymentId = PaymentId;
            booking.StripeClientSecretKey = stripeClientSecretKey;
            var res = await _stripeBaseRepository.UpdateAsync(booking);
            return res;
        }
    }
}
