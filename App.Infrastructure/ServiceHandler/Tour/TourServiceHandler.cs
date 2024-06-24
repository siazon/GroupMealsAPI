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

        Task<DbTour> CreateTour(int shopId);
        Task<DbTour> UpdateTour(DbTour tour, int shopId);
        Task<DbTour> GetTourById(string tourId);

    }

    public class TourServiceHandler : ITourServiceHandler
    {
        private readonly IDbCommonRepository<Domain.Holiday.DbTour> _tourRepository;
        private readonly IDbCommonRepository<DbShop> _shopRepository;
        private readonly IDateTimeUtil _dateTimeUtil;
        private readonly IEmailUtil _emailUtil;
        private readonly ILogManager _logger;
        private readonly IHolidayDataBuilder _holidayDataBuilder;
        private readonly IContentBuilder _contentBuilder;
        IHostingEnvironment _environment;

        public TourServiceHandler(IDbCommonRepository<Domain.Holiday.DbTour> tourRepository, IHostingEnvironment environment,  
            IDateTimeUtil dateTimeUtil, IEmailUtil emailUtil, ILogManager logger, IDbCommonRepository<DbShop> shopRepository, 
            IHolidayDataBuilder holidayDataBuilder, IContentBuilder contentBuilder)
        {
            _tourRepository = tourRepository;
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

            var tours = await _tourRepository.GetManyAsync(r => r.ShopId == shopId && r.IsActive == true);

            var compareDate = DateTime.Today.AddDays(1);

            var tourlist = tours.OrderByDescending(r => r.SortOrder).ToList();

            foreach (var tour in tourlist)
            {
                tour.AvailableDates = tour.AvailableDates.Where(r => r.HasValue && r.Value >= compareDate).ToList();
            }
            return tourlist;
        }

        public async Task<DbTour> GetTourById(string tourId)
        {
            var Booking = await _tourRepository.GetOneAsync(r => r.Id == tourId);
            return Booking;
        }
        public async Task<DbTour> CreateTour(int shopId)
        {
            DbTour tour = new DbTour();

            tour.Id = "T" + SnowflakeId.getSnowId();
            tour.Created = DateTime.UtcNow;
            tour.IsActive = true;
            tour.ShopId = shopId;

            var savedBooking = await _tourRepository.UpsertAsync(tour);

            return savedBooking;
        }

        public async Task<DbTour> UpdateTour(DbTour tour, int shopId)
        {
            Guard.NotNull(tour);

            var findTour = await _tourRepository.GetOneAsync(r => r.ShopId == shopId && r.Id == tour.Id);
            if (findTour == null)
                throw new ServiceException("Cannot Find tour");
            tour.Updated = DateTime.UtcNow;

            var saveTour = await _tourRepository.UpsertAsync(tour);

            return saveTour;
        }

    }
}