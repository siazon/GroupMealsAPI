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
using Stripe;
using App.Domain.Holiday;
using App.Infrastructure.Validation;
using App.Infrastructure.ServiceHandler.Tour;
using Hangfire;
using Microsoft.AspNetCore.Hosting;
using App.Infrastructure.Exceptions;

namespace App.Infrastructure.ServiceHandler.TravelMeals
{
    public interface ITrRestaurantBookingServiceHandler
    {
        Task<TrDbRestaurantBooking> GetBooking(string id);
        Task<bool> UpdateBooking(string billId, string productId, string priceId);
        Task<bool> BookingPaid(Stripe.Checkout.Session session);
        Task<bool> BookingPaid(string bookingId, string customerId = "", string payMethodId = "", string receiptUrl = "");
        Task<bool> UpdateStripeClientKey(string bookingId, string paymentId, string customerId, string secertKey);
        Task<TrDbRestaurantBooking> BindingPayInfoToTourBooking(string bookingId, string PaymentId, string stripeClientSecretKey);
        Task<bool> ResendEmail(int shopId, string bookingId);
    }
    public class TrRestaurantBookingServiceHandler : ITrRestaurantBookingServiceHandler
    {

        private readonly IDbCommonRepository<TrDbRestaurantBooking> _restaurantBookingRepository;
        private readonly IDbCommonRepository<StripeCheckoutSeesion> _stripeCheckoutSeesionRepository;
        private readonly IContentBuilder _contentBuilder;
        private readonly IEmailUtil _emailUtil;
        ILogManager _logger;
        IHostingEnvironment _environment;
        private readonly IDbCommonRepository<DbShop> _shopRepository;

        public TrRestaurantBookingServiceHandler(IDbCommonRepository<TrDbRestaurantBooking> restaurantBookingRepository, IDbCommonRepository<DbShop> shopRepository, IHostingEnvironment environment, IDbCommonRepository<StripeCheckoutSeesion> stripeCheckoutSeesionRepository, ILogManager logger, IContentBuilder contentBuilder, IEmailUtil emailUtil)
        {
            _restaurantBookingRepository = restaurantBookingRepository;
            _stripeCheckoutSeesionRepository = stripeCheckoutSeesionRepository;
            _contentBuilder = contentBuilder;
            _emailUtil = emailUtil;
            _logger = logger;
            _environment = environment;
            _shopRepository = shopRepository;
        }


        public async Task<TrDbRestaurantBooking> GetBooking(string id)
        {
            var Booking = await _restaurantBookingRepository.GetOneAsync(r => r.Id == id);
            return Booking;
        }

        public async Task<bool> UpdateBooking(string billId, string productId, string priceId)
        {
            TrDbRestaurantBooking booking = GetBooking(billId).Result;
            if (booking == null) return false;
            booking.StripeProductId = productId;
            booking.StripePriceId = priceId;
            var temp = await _restaurantBookingRepository.UpdateAsync(booking);
            return true;
        }


        public async Task<bool> BookingPaid(Stripe.Checkout.Session session)
        {
            _logger.LogInfo("BookingPaid");
            TrDbRestaurantBooking booking = await _restaurantBookingRepository.GetOneAsync(r => r.StripePriceId == session.Metadata["priceId"]);
            if (booking == null) return false;
            booking.Paid = true;

            _logger.LogInfo("BookingPaid" + booking.Id);
            var temp = await _restaurantBookingRepository.UpdateAsync(booking);

            _logger.LogInfo("BookingPaid.UpdateAsync" + temp.Id);
            var newItem = await _stripeCheckoutSeesionRepository.CreateAsync(new StripeCheckoutSeesion() { Data = session, BookingId = booking.Id });

            return true;
        }
        public async Task<bool> BookingPaid(string bookingId, string customerId = "", string payMethodId = "", string receiptUrl = "")
        {
            _logger.LogInfo("BookingPaid");
            TrDbRestaurantBooking booking = await _restaurantBookingRepository.GetOneAsync(r => r.Id == bookingId);
            if (booking == null)
            {
                _logger.LogInfo("bookingId: [" + bookingId + "] not found");
                return false;
            }

            if (!string.IsNullOrWhiteSpace(payMethodId))
                booking.StripePaymentId = payMethodId;
            if (!string.IsNullOrWhiteSpace(customerId))
            {
                booking.StripeCustomerId = customerId;
                booking.StripeSetupIntent = true;
            }
            if (!string.IsNullOrWhiteSpace(receiptUrl))
            {
                booking.StripeReceiptUrl = receiptUrl;
                booking.Paid = true;
                booking.Status = Domain.Enum.OrderStatusEnum.Paid;
            }
            _logger.LogInfo("BookingPaid" + booking.Id);
            var temp = await _restaurantBookingRepository.UpdateAsync(booking);
            var shopInfo =
         await _shopRepository.GetOneAsync(r => r.ShopId == 13 && r.IsActive.HasValue && r.IsActive.Value);
            if (shopInfo == null)
                throw new ServiceException("Cannot find shop info");
            EmailCustomer(booking, shopInfo);
            EmailBoss(booking, shopInfo,"New Order");
            //var newItem = await _stripeCheckoutSeesionRepository.CreateAsync(new StripeCheckoutSeesion() { Data = session, BookingId = booking.Id });

            return true;
        }
        private async Task EmailBoss(TrDbRestaurantBooking booking, DbShop shopInfo, string subject)
        {
            string wwwPath = this._environment.WebRootPath;
            string htmlTemp = EmailTemplateUtil.ReadTemplate(wwwPath, "new_meals_restaurant");
            string Detail = "";
            foreach (var item in booking.Details)
            {
                foreach (var course in item.Courses)
                {
                    Detail += course.MenuItemName + " * " + course.Qty + "人  ";
                }
            }
            var emailHtml = await _contentBuilder.BuildRazorContent(new { booking = booking,details= booking.Details[0],Detail }, htmlTemp);

            try
            {
                BackgroundJob.Enqueue<ITourBatchServiceHandler>(
                    s => s.SendEmail(shopInfo.ShopSettings, shopInfo.Email, shopInfo.Email, subject,
                        emailHtml));
            }
            catch (Exception ex)
            {
                _logger.LogError($"EmailBossError {ex.Message} -{ex.StackTrace} ");
            }
        }

        private async Task EmailCustomer(TrDbRestaurantBooking booking, DbShop shopInfo)
        {
            string wwwPath = this._environment.WebRootPath;
            string htmlTemp = EmailTemplateUtil.ReadTemplate(wwwPath, "new_meals");
            string Detail = "";
            foreach (var item in booking.Details)
            {
                Detail += item.RestaurantName+"       ";
                foreach (var course in item.Courses)
                {
                    Detail += course.MenuItemName + " * " + course.Qty+"人  " ;
                }
            }
            var emailHtml = await _contentBuilder.BuildRazorContent(new { booking = booking.Details[0], Detail }, htmlTemp);
            try
            {
                BackgroundJob.Enqueue<ITourBatchServiceHandler>(
                    s => s.SendEmail(shopInfo.ShopSettings, shopInfo.Email, booking.CustomerEmail, $"Thank you for your Booking",
                        emailHtml));
            }
            catch (Exception ex)
            {
                _logger.LogError($"EmailCustomer Email Customer Error {ex.Message} -{ex.StackTrace} ");
            }
        }
        public async Task<bool> ResendEmail(int shopId, string bookingId)
        {
            TrDbRestaurantBooking booking = await _restaurantBookingRepository.GetOneAsync(r => r.Id == bookingId);
            if (booking != null)
            {
                var shopInfo = await _shopRepository.GetOneAsync(r => r.ShopId == 13 && r.IsActive.HasValue && r.IsActive.Value);
                if (shopInfo == null)
                    throw new ServiceException("Cannot find shop info");
                EmailCustomer(booking, shopInfo);
            }
            return true;
        }
        public async Task<bool> UpdateStripeClientKey(string bookingId, string paymentId, string customerId, string secertKey)
        {
            TrDbRestaurantBooking booking = await _restaurantBookingRepository.GetOneAsync(r => r.Id == bookingId);
            if (booking == null)
            {
                _logger.LogInfo("bookingId: [" + bookingId + "] not found");
                return false;
            }

            if (!string.IsNullOrWhiteSpace(secertKey))
            {
                booking.StripeCustomerId = customerId;
                booking.StripeClientSecretKey = secertKey;
                booking.StripeSetupIntent = true;
                booking.StripePaymentId = paymentId;
            }
            var temp = await _restaurantBookingRepository.UpdateAsync(booking);

            //var newItem = await _stripeCheckoutSeesionRepository.CreateAsync(new StripeCheckoutSeesion() { Data = session, BookingId = booking.Id });

            return true;
        }
        public async Task<TrDbRestaurantBooking> BindingPayInfoToTourBooking(string bookingId, string PaymentId, string stripeClientSecretKey)
        {
            var booking = await _restaurantBookingRepository.GetOneAsync(r => r.Id == bookingId);
            Guard.NotNull(booking);
            booking.StripePaymentId = PaymentId;
            booking.StripeClientSecretKey = stripeClientSecretKey;
            var res = await _restaurantBookingRepository.UpdateAsync(booking);
            return res;
        }

    }
}
