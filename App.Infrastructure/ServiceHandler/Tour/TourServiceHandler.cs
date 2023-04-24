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
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace App.Infrastructure.ServiceHandler.Tour
{
    public interface ITourServiceHandler
    {
        Task<List<Domain.Holiday.Tour>> ListTours(int shopId);

        Task<TourBooking> RequestBooking(TourBooking booking, int shopId);
        Task<bool> BookingPaid(string bookingId, string customerId = "", string payMethodId = "", string receiptUrl = "");
        Task<TourBooking> GetTourBooking(string id);
    }

    public class TourServiceHandler : ITourServiceHandler
    {
        private readonly IDbCommonRepository<Domain.Holiday.Tour> _tourRepository;
        private readonly IDbCommonRepository<Domain.Holiday.TourBooking> _tourBookingRepository;
        private readonly IDbCommonRepository<DbShop> _shopRepository;
        private readonly IDateTimeUtil _dateTimeUtil;
        private readonly IEmailUtil _emailUtil;
        private readonly ILogManager _logger;
        private readonly IHolidayDataBuilder _holidayDataBuilder;
        private readonly IContentBuilder _contentBuilder;

        public TourServiceHandler(IDbCommonRepository<Domain.Holiday.Tour> tourRepository, IDbCommonRepository<TourBooking> tourBookingRepository, IDateTimeUtil dateTimeUtil, IEmailUtil emailUtil, ILogManager logger, IDbCommonRepository<DbShop> shopRepository, IHolidayDataBuilder holidayDataBuilder, IContentBuilder contentBuilder)
        {
            _tourRepository = tourRepository;
            _tourBookingRepository = tourBookingRepository;
            _dateTimeUtil = dateTimeUtil;
            _emailUtil = emailUtil;
            _logger = logger;
            _shopRepository = shopRepository;
            _holidayDataBuilder = holidayDataBuilder;
            _contentBuilder = contentBuilder;
        }

        public async Task<List<Domain.Holiday.Tour>> ListTours(int shopId)
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
        public async Task<TourBooking> GetTourBooking(string id) {
            var Booking = await _tourBookingRepository.GetOneAsync(r => r.Id == id);
            return Booking;
        }

        public async Task<TourBooking> RequestBooking(TourBooking booking, int shopId)
        {
            Guard.NotNull(booking);

            var findTour = await _tourRepository.GetOneAsync(r => r.ShopId == shopId && r.Id == booking.Tour.Id);
            if (findTour == null)
                throw new ServiceException("Cannot Find tour");

        

            var newBooking = booking.Clone();
            newBooking.Id = "IHO"+SnowflakeId.getSnowId();
            newBooking.Created = _dateTimeUtil.GetCurrentTime();
            newBooking.Ref = GuidHashUtil.Get6DigitNumber();

            var savedBooking =await _tourBookingRepository.CreateAsync(newBooking);

          

        
            return savedBooking;
        }

        public async Task<bool> BookingPaid(string bookingId, string customerId = "", string payMethodId = "", string receiptUrl = "")
        {
            _logger.LogInfo("BookingPaid");
            TourBooking booking = await _tourBookingRepository.GetOneAsync(r => r.Id == bookingId);
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
            }
            _logger.LogInfo("BookingPaid" + booking.Id);
            var temp = await _tourBookingRepository.UpdateAsync(booking);
            var shopInfo =
            await _shopRepository.GetOneAsync(r => r.ShopId == 11 && r.IsActive.HasValue && r.IsActive.Value);

            if (shopInfo == null)
                throw new ServiceException("Cannot find shop info");
            var dataset = _holidayDataBuilder.BuildContent(shopInfo, booking);
            //Add to Trello
            await EmailTrello(booking, shopInfo, dataset);

            //Email to Customer
            await EmailCustomer(booking, shopInfo, dataset);

            //Email to boss
            await EmailBoss(booking, shopInfo, dataset);

            return true;
        }

        private async Task EmailBoss(TourBooking booking, DbShop shopInfo, TourDataSet dataset)
        {
            var bossContent =
                shopInfo.ShopContents.FirstOrDefault(r => r.Key == EmailTemplateEnum.IReHolidayEmailBoss.ToString());
            var bossHtml = await _contentBuilder.BuildRazorContent(dataset, bossContent.Content);

            try
            {
                BackgroundJob.Enqueue<ITourBatchServiceHandler>(
                    s => s.SendEmail(shopInfo.ShopSettings, shopInfo.Email, shopInfo.Email, $"New Booking: {booking.Ref}",
                        bossHtml));
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestBooking Email Boss Error {ex.Message} -{ex.StackTrace} ");
            }
        }

        private async Task EmailCustomer(TourBooking booking, DbShop shopInfo, TourDataSet dataset)
        {
            var customerContent =
                shopInfo.ShopContents.FirstOrDefault(r => r.Key == EmailTemplateEnum.IReHolidayEmailCustomer.ToString());
            var customerHtml = await _contentBuilder.BuildRazorContent(dataset, customerContent.Content);

            try
            {
                BackgroundJob.Enqueue<ITourBatchServiceHandler>(
                    s => s.SendEmail(shopInfo.ShopSettings, shopInfo.Email, booking.Email, $"Thank you for your Booking: {booking.Ref}",
                        customerHtml));
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestBooking Email Customer Error {ex.Message} -{ex.StackTrace} ");
            }
        }

        private async Task EmailTrello(TourBooking booking, DbShop shopInfo, TourDataSet dataset)
        {
            var content =
                shopInfo.ShopContents.FirstOrDefault(r => r.Key == EmailTemplateEnum.IReHolidayEmailTrello.ToString());
            var trelloHtml = await _contentBuilder.BuildRazorContent(dataset, content.Content);

            try
            {
                BackgroundJob.Enqueue<ITourBatchServiceHandler>(
                    s => s.SendEmail(shopInfo.ShopSettings, shopInfo.Email, shopInfo.ContactEmail, $"New Booking: {booking.Ref}",
                        trelloHtml));
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestBooking Email Trello Error {ex.Message} -{ex.StackTrace} ");
            }
        }
    }
}