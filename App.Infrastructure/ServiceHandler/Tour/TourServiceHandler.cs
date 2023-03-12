using App.Domain.Common.Shop;
using App.Domain.Enum;
using App.Domain.Holiday;
using App.Infrastructure.Builders.Common;
using App.Infrastructure.Builders.IreHoliday;
using App.Infrastructure.Exceptions;
using App.Infrastructure.Repository;
using App.Infrastructure.Utility.Common;
using App.Infrastructure.Validation;
using Hangfire;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace App.Infrastructure.ServiceHandler.Tour
{
    public interface ITourServiceHandler
    {
        Task<List<Domain.Holiday.Tour>> ListTours(int shopId);

        Task<TourBooking> RequestBooking(TourBooking booking, int shopId);
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

        public async Task<TourBooking> RequestBooking(TourBooking booking, int shopId)
        {
            Guard.NotNull(booking);

            var findTour = await _tourRepository.GetOneAsync(r => r.ShopId == shopId && r.Id == booking.Tour.Id);
            if (findTour == null)
                throw new ServiceException("Cannot Find tour");

            var shopInfo =
                await _shopRepository.GetOneAsync(r => r.ShopId == shopId && r.IsActive.HasValue && r.IsActive.Value);

            if (shopInfo == null)
                throw new ServiceException("Cannot find shop info");

            var newBooking = booking.Clone();
            newBooking.Id = Guid.NewGuid().ToString();
            newBooking.Created = _dateTimeUtil.GetCurrentTime();
            newBooking.Ref = GuidHashUtil.Get6DigitNumber();

            var savedBooking = await _tourBookingRepository.CreateAsync(newBooking);

            var dataset = _holidayDataBuilder.BuildContent(shopInfo, booking);

            //Add to Trello
            await EmailTrello(booking, shopInfo, dataset);

            //Email to Customer
            await EmailCustomer(booking, shopInfo, dataset);

            //Email to boss
            await EmailBoss(booking, shopInfo, dataset);

            return savedBooking;
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