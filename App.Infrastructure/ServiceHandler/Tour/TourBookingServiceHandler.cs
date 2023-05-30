using App.Domain.Common.Shop;
using App.Domain.Enum;
using App.Domain.Holiday;
using App.Domain.TravelMeals;
using App.Infrastructure.Builders.Common;
using App.Infrastructure.Builders.IreHoliday;
using App.Infrastructure.Exceptions;
using App.Infrastructure.Repository;
using App.Infrastructure.Utility.Common;
using App.Infrastructure.Validation;
using Hangfire;
using Microsoft.AspNetCore.Hosting;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Infrastructure.ServiceHandler.Tour
{
    public interface ITourBookingServiceHandler
    {

        Task<TourBooking> RequestBooking(TourBooking booking, int shopId);
        Task<bool> BookingPaid(string bookingId, string customerId = "", string chargeId = "", string payMethodId = "", string receiptUrl = "");
        Task<TourBooking> GetTourBooking(string id);
        Task<List<TourBooking>> GetTourBookings(string code, string email);
        Task<List<TourBooking>> GetTourBookingsByAdmin(string code);
        Task<bool> DeleteTourBookingById(string code);
        Task<TourBooking> UpdateTourBooking(TourBooking booking);
        Task<bool> BookingRefund(string chargeId);
        Task<TourBooking> TourBookingRefundApply(string id);
        Task<TourBooking> BindingPayInfoToTourBooking(string bookingId, string PaymentId, string stripeClientSecretKey);
        Task EmailCustomerForRefund(TourBooking booking);

    }

    public class TourBookingServiceHandler : ITourBookingServiceHandler
    {
        private readonly IDbCommonRepository<DbTour> _tourRepository;
        private readonly IDbCommonRepository<TourBooking> _tourBookingRepository;
        private readonly IDbCommonRepository<DbShop> _shopRepository;
        private readonly IDateTimeUtil _dateTimeUtil;
        private readonly IEmailUtil _emailUtil;
        private readonly ILogManager _logger;
        private readonly IHolidayDataBuilder _holidayDataBuilder;
        private readonly IContentBuilder _contentBuilder;
        IHostingEnvironment _environment;

        public TourBookingServiceHandler(IDbCommonRepository<Domain.Holiday.DbTour> tourRepository, IHostingEnvironment environment, IDbCommonRepository<TourBooking> tourBookingRepository, IDateTimeUtil dateTimeUtil, IEmailUtil emailUtil, ILogManager logger, IDbCommonRepository<DbShop> shopRepository, IHolidayDataBuilder holidayDataBuilder, IContentBuilder contentBuilder)
        {
            _tourRepository = tourRepository;
            _tourBookingRepository = tourBookingRepository;
            _dateTimeUtil = dateTimeUtil;
            _emailUtil = emailUtil;
            _logger = logger;
            _shopRepository = shopRepository;
            _holidayDataBuilder = holidayDataBuilder;
            _contentBuilder = contentBuilder;
            _environment = environment;
        }

        public async Task<TourBooking> RequestBooking(TourBooking booking, int shopId)
        {
            Guard.NotNull(booking);

            var findTour = await _tourRepository.GetOneAsync(r => r.ShopId == shopId && r.Id == booking.Tour.Id);
            if (findTour == null)
                throw new ServiceException("Cannot Find tour");
            TourBooking newBooking;
            var createTime = _dateTimeUtil.GetCurrentTime();
            var exsitBookings = await _tourBookingRepository.GetManyAsync(r => r.Status == OrderStatusEnum.None && r.Email == booking.Email);
            var exsitBooking = exsitBookings.FirstOrDefault(a => (createTime - a.Created).Value.Hours < 2);
            if (exsitBooking != null)
            {
                exsitBooking.Created = _dateTimeUtil.GetCurrentTime();
                exsitBooking.NumberOfPeople = booking.NumberOfPeople;
                exsitBooking.NumberOfChild = booking.NumberOfChild;
                exsitBooking.NumberOfAgedOrStudent = booking.NumberOfAgedOrStudent;
                exsitBooking.PhoneNumber = booking.PhoneNumber;
                exsitBooking.SelectDate = booking.SelectDate;
                exsitBooking.Name = booking.Name;
                exsitBooking.Tour=booking.Tour;
                var savedBooking = await _tourBookingRepository.UpdateAsync(exsitBooking);

                return savedBooking;
            }
            else
            {
                newBooking = booking.Clone();
                newBooking.Id = "IHO" + SnowflakeId.getSnowId();
                newBooking.Created = _dateTimeUtil.GetCurrentTime();
                newBooking.Ref = GuidHashUtil.Get6DigitNumber();

                var savedBooking = await _tourBookingRepository.CreateAsync(newBooking);

                return savedBooking;
            }
        }

        public async Task<bool> BookingPaid(string bookingId, string customerId = "", string chargeId = "", string payMethodId = "", string receiptUrl = "")
        {
            _logger.LogInfo("BookingPaid");
            TourBooking booking = await _tourBookingRepository.GetOneAsync(r => r.Id == bookingId);
            if (booking == null)
            {
                _logger.LogInfo("bookingId: [" + bookingId + "] not found");
                return false;
            }
            if (!string.IsNullOrWhiteSpace(chargeId))
                booking.StripeChargeId = chargeId;
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
                booking.Status = OrderStatusEnum.Paid;
            }
            _logger.LogInfo("BookingPaid" + booking.Id);
            var temp = await _tourBookingRepository.UpdateAsync(booking);
            var shopInfo =
            await _shopRepository.GetOneAsync(r => r.ShopId == 13 && r.IsActive.HasValue && r.IsActive.Value);

            if (shopInfo == null)
                throw new ServiceException("Cannot find shop info");
            var dataset = _holidayDataBuilder.BuildContent(shopInfo, booking);

            //Email to Customer
            await EmailCustomer(booking, shopInfo);

            //Email to boss
            await EmailBoss(booking, shopInfo, $"New Booking: {booking.Ref}");
            return true;
        }
        public async Task<TourBooking> GetTourBooking(string id)
        {
            var Booking = await _tourBookingRepository.GetOneAsync(r => r.Id == id);
            return Booking;
        }
        public async Task<bool> BookingRefund(string chargeId)
        {
            TourBooking booking = await _tourBookingRepository.GetOneAsync(r => r.StripeChargeId == chargeId);
            if (booking == null)
            {
                _logger.LogInfo("chargeId: [" + chargeId + "] not found");
                return false;
            }

            booking.Paid = false;
            booking.Status = OrderStatusEnum.Refunded;
            _logger.LogInfo("BookingPaid" + booking.Id);
            var temp = await _tourBookingRepository.UpdateAsync(booking);
            //var shopInfo =
            //await _shopRepository.GetOneAsync(r => r.ShopId == 13 && r.IsActive.HasValue && r.IsActive.Value);

            //if (shopInfo == null)
            //    throw new ServiceException("Cannot find shop info");
            //var dataset = _holidayDataBuilder.BuildContent(shopInfo, booking);

            ////Email to Customer
            //await EmailCustomer(booking, shopInfo);

            ////Email to boss
            //await EmailBoss(booking, shopInfo);
            return true;
        }


        private async Task EmailBoss(TourBooking booking, DbShop shopInfo, string subject)
        {
            string wwwPath = this._environment.WebRootPath;
            string htmlTemp = EmailTemplateUtil.ReadTemplate(wwwPath, "system_message");
            var emailHtml = await _contentBuilder.BuildRazorContent(booking, htmlTemp);

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

        private async Task EmailCustomer(TourBooking booking, DbShop shopInfo)
        {
            string wwwPath = this._environment.WebRootPath;
            string htmlTemp = EmailTemplateUtil.ReadTemplate(wwwPath, "tour_ticket");
            decimal amount = (booking.NumberOfPeople ?? 0) * (booking.Tour.Price ?? 0) +
                 (booking.NumberOfAgedOrStudent ?? 0) * (booking.Tour.ConcessionPrice ?? 0) +
                 (booking.NumberOfChild ?? 0) * (booking.Tour.ChildPrice ?? 0);
            var emailHtml = await _contentBuilder.BuildRazorContent(new { booking, amount }, htmlTemp);
            try
            {
                BackgroundJob.Enqueue<ITourBatchServiceHandler>(
                    s => s.SendEmail(shopInfo.ShopSettings, shopInfo.Email, booking.Email, $"Thank you for your Booking: {booking.Ref}",
                        emailHtml));
            }
            catch (Exception ex)
            {
                _logger.LogError($"EmailCustomer Email Customer Error {ex.Message} -{ex.StackTrace} ");
            }
        }
        public async Task EmailCustomerForRefund(TourBooking booking)
        {
            if (booking == null)
                throw new ServiceException("Cannot find booking info");
            var shopInfo = await _shopRepository.GetOneAsync(r => r.ShopId == 13 && r.IsActive.HasValue && r.IsActive.Value);
            if (shopInfo == null)
                throw new ServiceException("Cannot find shop info");
            string wwwPath = this._environment.WebRootPath;
            string htmlTemp = EmailTemplateUtil.ReadTemplate(wwwPath, "refunded_msg");
            decimal amount = (booking.NumberOfPeople ?? 0) * (booking.Tour.Price ?? 0) +
                 (booking.NumberOfAgedOrStudent ?? 0) * (booking.Tour.ConcessionPrice ?? 0) +
                 (booking.NumberOfChild ?? 0) * (booking.Tour.ChildPrice ?? 0);
            var emailHtml = await _contentBuilder.BuildRazorContent(new { booking, amount }, htmlTemp);
            try
            {
                BackgroundJob.Enqueue<ITourBatchServiceHandler>(
                    s => s.SendEmail(shopInfo.ShopSettings, shopInfo.Email, booking.Email, $"Your order has been refunded: {booking.Tour.TourInfo.NameEn}",
                        emailHtml));
            }
            catch (Exception ex)
            {
                _logger.LogError($"EmailCustomer Email Customer Error {ex.Message} -{ex.StackTrace} ");
            }
        }

        public async Task<List<TourBooking>> GetTourBookings(string code, string email)
        {
            var bookings = await _tourBookingRepository.GetManyAsync(r => r.Ref == code && r.Email == email);
            var tickets = bookings.ToList();
            return tickets;
        }
        public async Task<List<TourBooking>> GetTourBookingsByAdmin(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                var bookings = await _tourBookingRepository.GetManyAsync(r => 1 == 1);
                var tickets = bookings.ToList().FindAll(a => a.Status != OrderStatusEnum.Disable);
                return tickets;
            }
            else
            {
                var bookings = await _tourBookingRepository.GetManyAsync(r => r.Email == code);
                var tickets = bookings.ToList().FindAll(a => a.Status != OrderStatusEnum.Disable);
                return tickets;
            }

        }
        public async Task<bool> DeleteTourBookingById(string Id)
        {
            var booking = await _tourBookingRepository.GetOneAsync(r => r.Id == Id);
            var res = await _tourBookingRepository.DeleteAsync(booking);
            return res != null;

        }
        public async Task<TourBooking> UpdateTourBooking(TourBooking booking)
        {
            Guard.NotNull(booking);
            var res = await _tourBookingRepository.UpdateAsync(booking);
            return res;
        }
        public async Task<TourBooking> BindingPayInfoToTourBooking(string bookingId, string PaymentId, string stripeClientSecretKey)
        {
            var booking = await _tourBookingRepository.GetOneAsync(r => r.Id == bookingId);
            Guard.NotNull(booking);
            booking.StripePaymentId = PaymentId;
            booking.StripeClientSecretKey = stripeClientSecretKey;
            var res = await _tourBookingRepository.UpdateAsync(booking);
            return res;
        }

        public async Task<TourBooking> TourBookingRefundApply(string id)
        {
            var booking = await _tourBookingRepository.GetOneAsync(r => r.Id == id);
            booking.Status = OrderStatusEnum.ApplyRefund;
            var res = await _tourBookingRepository.UpdateAsync(booking);

            var shopInfo =
            await _shopRepository.GetOneAsync(r => r.ShopId == 13 && r.IsActive.HasValue && r.IsActive.Value);
            await EmailBoss(booking, shopInfo, $"New Refund: {booking.Ref}");

            return res;
        }

    }
}