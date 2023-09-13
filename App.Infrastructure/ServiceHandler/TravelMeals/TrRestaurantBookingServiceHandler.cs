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
        Task<bool> BookingPaid(string bookingId, string customerId = "", string chargeId = "", string payMethodId = "", string receiptUrl = "");
        Task<bool> UpdateStripeClientKey(string bookingId, string paymentId, string customerId, string secertKey);
        Task<TrDbRestaurantBooking> BindingPayInfoToTourBooking(TrDbRestaurantBooking gpBooking, string PaymentId, string stripeClientSecretKey,bool isSetupPay);
        Task<bool> ResendEmail(string bookingId);
        Task<bool> UpdateAccepted(string bookingId,int acceptType);
        Task<bool> UpdateAcceptedReason(string bookingId, string reason);
    }
    public class TrRestaurantBookingServiceHandler : ITrRestaurantBookingServiceHandler
    {

        private readonly IDbCommonRepository<TrDbRestaurantBooking> _restaurantBookingRepository;
        private readonly IDbCommonRepository<StripeCheckoutSeesion> _stripeCheckoutSeesionRepository;
        private readonly IContentBuilder _contentBuilder;
        private readonly IEmailUtil _emailUtil;
        ILogManager _logger;
        ITwilioUtil _twilioUtil;
        IStripeUtil _stripeUtil;
        IHostingEnvironment _environment;
        private readonly IDbCommonRepository<DbShop> _shopRepository;

        public TrRestaurantBookingServiceHandler(ITwilioUtil twilioUtil, IDbCommonRepository<TrDbRestaurantBooking> restaurantBookingRepository, 
            IDbCommonRepository<DbShop> shopRepository, IHostingEnvironment environment, IStripeUtil stripeUtil,
            IDbCommonRepository<StripeCheckoutSeesion> stripeCheckoutSeesionRepository, 
            ILogManager logger, IContentBuilder contentBuilder, IEmailUtil emailUtil)
        {
            _restaurantBookingRepository = restaurantBookingRepository;
            _stripeCheckoutSeesionRepository = stripeCheckoutSeesionRepository;
            _contentBuilder = contentBuilder;
            _emailUtil = emailUtil;
            _logger = logger;
            _twilioUtil = twilioUtil;
            _environment = environment;
            _shopRepository = shopRepository;
            _stripeUtil = stripeUtil;
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
        public async Task<bool> UpdateAccepted(string billId, int acceptType)
        {
            TrDbRestaurantBooking booking = GetBooking(billId).Result;
            if (booking == null) return false;
            booking.Accepted = acceptType;
            if (acceptType == 2) {
                _stripeUtil.RefundGroupMeals(booking);
            }
                var temp = await _restaurantBookingRepository.UpdateAsync(booking);
            string msg = $"您于{booking.BookingDate} {booking.BookingTime} 提交的订单已被接收，请按时就餐";
            if (acceptType == 2)
                msg =  $"您于{booking.BookingDate} {booking.BookingTime} 提交的订单已被拒绝，请登录groupmeals.com查询别的餐厅";
            //_twilioUtil.sendSMS(booking.CustomerPhone, msg);
            var shopInfo = await _shopRepository.GetOneAsync(r => r.ShopId == 13 && r.IsActive.HasValue && r.IsActive.Value);
            if (shopInfo == null)
            {
                _logger.LogInfo("----------------Cannot find shop info" + booking.Id);
                throw new ServiceException("Cannot find shop info");
            }
            if (acceptType == 1)
                EmailCustomer(booking, shopInfo, "new_meals_confirm");
            else if (acceptType == 2)
            {
                EmailCustomer(booking, shopInfo, "new_meals_decline");
            }
            return true;
        }
        public async Task<bool> UpdateAcceptedReason(string billId, string reason)
        {
            TrDbRestaurantBooking booking = GetBooking(billId).Result;
            if (booking == null) return false;
            booking.AcceptReason = reason;
            var temp = await _restaurantBookingRepository.UpdateAsync(booking);
            return true;
        }

        public async Task<bool> BookingPaid(Stripe.Checkout.Session session)
        {
            try
            {
                _logger.LogInfo("BookingPaid.session");
                TrDbRestaurantBooking booking = await _restaurantBookingRepository.GetOneAsync(r => r.StripePriceId == session.Metadata["priceId"]);
                if (booking == null) return false;
                booking.Paid = true;

                _logger.LogInfo("BookingPaid.session.id:" + booking.Id);
                var temp = await _restaurantBookingRepository.UpdateAsync(booking);

                _logger.LogInfo("BookingPaid.UpdateAsync.session" + temp.Id);
                var newItem = await _stripeCheckoutSeesionRepository.CreateAsync(new StripeCheckoutSeesion() { Data = session, BookingId = booking.Id });
            }
            catch (Exception ex)
            {
                _logger.LogInfo("BookingPaid.session.err" + ex);
            }
            return true;
        }
        public async Task<bool> BookingPaid(string bookingId, string customerId = "",string chargeId="",string payMethodId = "", string receiptUrl = "")
        {
            try
            {

          
            _logger.LogInfo("----------------BookingPaid");
            TrDbRestaurantBooking booking = await _restaurantBookingRepository.GetOneAsync(r => r.Id == bookingId);
            if (booking == null)
            {
                _logger.LogInfo("----------------bookingId: [" + bookingId + "] not found");
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

                    booking.StripeChargeId = chargeId;
                    booking.StripeReceiptUrl = receiptUrl;
                booking.Paid = true;
                booking.Status = Domain.Enum.OrderStatusEnum.Paid;
            }
            _logger.LogInfo("----------------BookingPaid" + booking.Id);
            var temp = await _restaurantBookingRepository.UpdateAsync(booking);
            var shopInfo =
         await _shopRepository.GetOneAsync(r => r.ShopId == 13 && r.IsActive.HasValue && r.IsActive.Value);
            if (shopInfo == null)
            {
                _logger.LogInfo("----------------Cannot find shop info" + booking.Id);
                throw new ServiceException("Cannot find shop info");
            }
            EmailCustomer(booking, shopInfo, "new_meals");
            EmailBoss(booking, shopInfo, "New Order");
                //var newItem = await _stripeCheckoutSeesionRepository.CreateAsync(new StripeCheckoutSeesion() { Data = session, BookingId = booking.Id });
            }
            catch (Exception ex)
            {
                _logger.LogInfo("----------------BookingPaid.err" + ex.Message+"： "+ex.StackTrace);
            }
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
                    Detail += course.MenuItemName + " * " + course.Qty + "人  €" + course.Amount;
                }
            }
            _twilioUtil.sendSMS(booking.Details[0].RestaurantPhone, "You got a new order. Please see details in groupmeals.com");
            var emailHtml = await _contentBuilder.BuildRazorContent(new { booking = booking, details = booking.Details[0], Detail, Memo=booking.Details[0].Courses[0].Memo }, htmlTemp);

            try
            {
                BackgroundJob.Enqueue<ITourBatchServiceHandler>(
                    s => s.SendEmail(shopInfo.ShopSettings, shopInfo.Email, booking.Details[0].RestaurantEmail, subject,
                        emailHtml));

            }
            catch (Exception ex)
            {
                _logger.LogError($"EmailBossError {ex.Message} -{ex.StackTrace} ");
            }
        }

        private async Task EmailCustomer(TrDbRestaurantBooking booking, DbShop shopInfo,string tempName)
        {
            string wwwPath = this._environment.WebRootPath;
            string htmlTemp = EmailTemplateUtil.ReadTemplate(wwwPath, tempName);
            string Detail = "";
            foreach (var item in booking.Details)
            {
                Detail += item.RestaurantName + "       ";
                foreach (var course in item.Courses)
                {
                    Detail += course.MenuItemName + " * " + course.Qty + "人  €" + course.Amount;
                }
            }
            var emailHtml = "";
            try
            {
                emailHtml = await _contentBuilder.BuildRazorContent(new { booking = booking.Details[0], Detail }, htmlTemp);
            }
            catch (Exception ex)
            {
                _logger.LogError("----------------emailHtml---error" + ex.Message);
            }
            if (string.IsNullOrWhiteSpace(emailHtml))
            {
                return;
            }
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
        public async Task<bool> ResendEmail(string bookingId)
        {
            try
            {
                TrDbRestaurantBooking booking = await _restaurantBookingRepository.GetOneAsync(r => r.Id == bookingId);
                if (booking != null)
                {
                    var shopInfo = await _shopRepository.GetOneAsync(r => r.ShopId == 13 && r.IsActive.HasValue && r.IsActive.Value);
                    if (shopInfo == null)
                    {
                        throw new ServiceException("Cannot find shop info");
                    }
                    EmailBoss(booking, shopInfo,"new Order");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("----------------err" + ex);
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
        public async Task<TrDbRestaurantBooking> BindingPayInfoToTourBooking(TrDbRestaurantBooking booking, string PaymentId, string stripeClientSecretKey, bool isSetupPay)
        {
            Guard.NotNull(booking);
            booking.StripePaymentId = PaymentId;
            booking.SetupPay=isSetupPay;
            booking.StripeClientSecretKey = stripeClientSecretKey;
            var res = await _restaurantBookingRepository.UpdateAsync(booking);
            return res;
        }

    }
}
