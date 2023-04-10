using App.Domain.TravelMeals.Restaurant;
using App.Domain.TravelMeals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using App.Domain.Common.Shop;
using App.Infrastructure.Builders.Common;
using App.Infrastructure.Builders.TravelMeals;
using App.Infrastructure.Repository;
using App.Infrastructure.Utility.Common;
using App.Domain.Common.Stripe;

namespace App.Infrastructure.ServiceHandler.TravelMeals
{
    public interface ITrRestaurantBookingServiceHandler
    {
        Task<TrDbRestaurantBooking> GetBooking(int shopId, string id);
        Task<bool> UpdateBooking(int shopId,string billId, string productId, string priceId);
        Task<bool> BookingPaid(Stripe.Checkout.Session session);
    }
    public class TrRestaurantBookingServiceHandler : ITrRestaurantBookingServiceHandler
    {

        private readonly IDbCommonRepository<TrDbRestaurantBooking> _restaurantBookingRepository;
        private readonly IDbCommonRepository<StripeCheckoutSeesion> _stripeCheckoutSeesionRepository;
        private readonly IContentBuilder _contentBuilder;
        private readonly IEmailUtil _emailUtil;

        public TrRestaurantBookingServiceHandler( IDbCommonRepository<TrDbRestaurantBooking> restaurantBookingRepository, IDbCommonRepository<StripeCheckoutSeesion> stripeCheckoutSeesionRepository, IContentBuilder contentBuilder, IEmailUtil emailUtil)
        {
            _restaurantBookingRepository = restaurantBookingRepository;
            _stripeCheckoutSeesionRepository= stripeCheckoutSeesionRepository;
            _contentBuilder = contentBuilder;
            _emailUtil = emailUtil;
        }


        public async Task<TrDbRestaurantBooking> GetBooking(int shopId, string id)
        {
            var Booking = await _restaurantBookingRepository.GetOneAsync(r => r.Id == id );
            return Booking;
        }

        public async  Task<bool> UpdateBooking(int shopId, string billId, string productId, string priceId)
        {
            TrDbRestaurantBooking booking = GetBooking(shopId, billId).Result;
            if(booking == null) return false;
            booking.StripeProductId = productId;
            booking.StripePriceId = priceId;
          var temp=  await _restaurantBookingRepository.UpdateAsync(booking);
            return true;
        }


        public async Task<bool> BookingPaid(Stripe.Checkout.Session session)
        {
            TrDbRestaurantBooking booking = await _restaurantBookingRepository.GetOneAsync(r => r.StripePriceId == session.Metadata["priceId"]);
            if (booking == null) return false;
            booking.IsPaid = true;
            var temp = await _restaurantBookingRepository.UpdateAsync(booking);

            var newItem = await _stripeCheckoutSeesionRepository.CreateAsync(new StripeCheckoutSeesion() {Data=session,BookingId=booking.Id });

            return true;
        }
    }
}
