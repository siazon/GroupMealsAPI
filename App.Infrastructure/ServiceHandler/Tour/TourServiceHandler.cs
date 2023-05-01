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
    public interface ITourServiceHandler
    {
        Task<List<Domain.Holiday.DbTour>> ListTours(int shopId);

        Task<TourBooking> RequestBooking(TourBooking booking, int shopId);
        Task<DbTour> CreateTour(DbTour tour, int shopId);
        Task<bool> BookingPaid(string bookingId, string customerId = "", string chargeId = "", string payMethodId = "", string receiptUrl = "");
        Task<TourBooking> GetTourBooking(string id);
        Task<List<TourBooking>> GetTourBookings(string code,string email);
        Task<List<TourBooking>> GetTourBookingsByAdmin(string code);
        Task<bool> DeleteTourBookingById(string code);
        Task<TourBooking> UpdateTourBooking(TourBooking booking);

    }

    public class TourServiceHandler : ITourServiceHandler
    {
        private readonly IDbCommonRepository<Domain.Holiday.DbTour> _tourRepository;
        private readonly IDbCommonRepository<Domain.Holiday.TourBooking> _tourBookingRepository;
        private readonly IDbCommonRepository<DbShop> _shopRepository;
        private readonly IDateTimeUtil _dateTimeUtil;
        private readonly IEmailUtil _emailUtil;
        private readonly ILogManager _logger;
        private readonly IHolidayDataBuilder _holidayDataBuilder;
        private readonly IContentBuilder _contentBuilder;
        IHostingEnvironment _environment;

        public TourServiceHandler(IDbCommonRepository<Domain.Holiday.DbTour> tourRepository, IHostingEnvironment environment, IDbCommonRepository<TourBooking> tourBookingRepository, IDateTimeUtil dateTimeUtil, IEmailUtil emailUtil, ILogManager logger, IDbCommonRepository<DbShop> shopRepository, IHolidayDataBuilder holidayDataBuilder, IContentBuilder contentBuilder)
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

        public async Task<List<Domain.Holiday.DbTour>> ListTours(int shopId)
        {

            var tours = await _tourRepository.GetManyAsync(r => r.ShopId == shopId);

            var compareDate = DateTime.Today.AddDays(1);

            var tourlist = tours.OrderByDescending(r => r.SortOrder).ToList();

            foreach (var tour in tourlist)
            {
                tour.AvailableDates = tour.AvailableDates.Where(r => r.HasValue && r.Value >= compareDate).ToList();
            }
            return tourlist;
        }
        public async Task<TourBooking> GetTourBooking(string id)
        {
            var Booking = await _tourBookingRepository.GetOneAsync(r => r.Id == id);
            return Booking;
        }
        public async Task<DbTour> CreateTour(DbTour tour, int shopId)
        {
            Guard.NotNull(tour);

            var findTour = await _tourRepository.GetOneAsync(r => r.ShopId == shopId && r.Id == tour.Id);
            if (findTour == null)
                throw new ServiceException("Cannot Find tour");
            var newBooking = tour.DeepClone();
            newBooking.Id = "T" + SnowflakeId.getSnowId();
            newBooking.Created = _dateTimeUtil.GetCurrentTime();

            var savedBooking = await _tourRepository.CreateAsync(newBooking);

            return savedBooking;
        }

        public async Task<TourBooking> RequestBooking(TourBooking booking, int shopId)
        {
            Guard.NotNull(booking);

            var findTour = await _tourRepository.GetOneAsync(r => r.ShopId == shopId && r.Id == booking.Tour.Id);
            if (findTour == null)
                throw new ServiceException("Cannot Find tour");



            var newBooking = booking.Clone();
            newBooking.Id = "IHO" + SnowflakeId.getSnowId();
            newBooking.Created = _dateTimeUtil.GetCurrentTime();
            newBooking.Ref = GuidHashUtil.Get6DigitNumber();

            var savedBooking = await _tourBookingRepository.CreateAsync(newBooking);




            return savedBooking;
        }

        public async Task<bool> BookingPaid(string bookingId, string customerId = "",string chargeId="", string payMethodId = "", string receiptUrl = "")
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
            await EmailBoss(booking, shopInfo); 
            return true;
        }



        private async Task EmailBoss(TourBooking booking, DbShop shopInfo)
        {
            string wwwPath = this._environment.WebRootPath;
            string htmlTemp = EmailTemplateUtil.ReadTemplate(wwwPath, "tour_ticket");
            var emailHtml = await _contentBuilder.BuildRazorContent(booking, htmlTemp);

            try
            {
                BackgroundJob.Enqueue<ITourBatchServiceHandler>(
                    s => s.SendEmail(shopInfo.ShopSettings, shopInfo.Email, shopInfo.Email, $"New Booking: {booking.Ref}",
                        emailHtml));
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestBooking Email Boss Error {ex.Message} -{ex.StackTrace} ");
            }
        }

        private async Task EmailCustomer(TourBooking booking, DbShop shopInfo )
        {
            string wwwPath = this._environment.WebRootPath;
            string htmlTemp = EmailTemplateUtil.ReadTemplate(wwwPath, "tour_ticket");
            var emailHtml = await _contentBuilder.BuildRazorContent(booking, htmlTemp);
            try
            {
                BackgroundJob.Enqueue<ITourBatchServiceHandler>(
                    s => s.SendEmail(shopInfo.ShopSettings, shopInfo.Email, booking.Email, $"Thank you for your Booking: {booking.Ref}",
                        emailHtml));
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestBooking Email Customer Error {ex.Message} -{ex.StackTrace} ");
            }
        }

        public async Task<List<TourBooking>> GetTourBookings(string code, string email)
        {
            var bookings = await _tourBookingRepository.GetManyAsync(r => r.Ref == code&&r.Email==email);
            var tickets = bookings.ToList();
            return tickets;
        }
        public async Task<List<TourBooking>> GetTourBookingsByAdmin(string code)
        {
            var bookings = await _tourBookingRepository.GetManyAsync(r => r.Email == code);
            var tickets = bookings.ToList();
            return tickets;
        }
        public async Task<bool> DeleteTourBookingById(string Id) {
            var booking=await _tourBookingRepository.GetOneAsync(r=>r.Id==Id);
            var res = await _tourBookingRepository.DeleteAsync(booking);
            return res!=null;

        }
        public async Task<TourBooking> UpdateTourBooking(TourBooking booking)
        {
            Guard.NotNull(booking);
            var res=await _tourBookingRepository.UpdateAsync(booking);
            return res;
        }
    }
}