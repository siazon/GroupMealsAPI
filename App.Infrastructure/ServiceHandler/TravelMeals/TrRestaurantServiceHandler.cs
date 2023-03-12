using App.Domain.Common.Content;
using App.Domain.Common.Setting;
using App.Domain.Common.Shop;
using App.Domain.TravelMeals;
using App.Domain.TravelMeals.Restaurant;
using App.Infrastructure.Builders.Common;
using App.Infrastructure.Builders.TravelMeals;
using App.Infrastructure.Exceptions;
using App.Infrastructure.Repository;
using App.Infrastructure.Utility.Common;
using App.Infrastructure.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using App.Domain.Enum;

namespace App.Infrastructure.ServiceHandler.TravelMeals
{
    public interface ITrRestaurantServiceHandler
    {
        Task<List<TrDbRestaurant>> GetRestaurantInfo(int shopId);

        Task<TrDbRestaurantBooking> RequestBooking(TrDbRestaurantBooking booking, int shopId);
    }

    public class TrRestaurantServiceHandler : ITrRestaurantServiceHandler
    {
        private readonly IDbCommonRepository<TrDbRestaurant> _restaurantRepository;
        private readonly IDateTimeUtil _dateTimeUtil;
        private readonly IDbCommonRepository<DbShop> _shopRepository;
        private readonly IDbCommonRepository<DbShopContent> _shopContentRepository;
        private readonly IDbCommonRepository<DbSetting> _settingRepository;
        private readonly ITrBookingDataSetBuilder _bookingDataSetBuilder;
        private readonly IDbCommonRepository<TrDbRestaurantBooking> _restaurantBookingRepository;
        private readonly IContentBuilder _contentBuilder;
        private readonly IEmailUtil _emailUtil;

        public TrRestaurantServiceHandler(IDbCommonRepository<TrDbRestaurant> restaurantRepository, IDateTimeUtil dateTimeUtil, IDbCommonRepository<DbShop> shopRepository, IDbCommonRepository<DbShopContent> shopContentRepository, IDbCommonRepository<DbSetting> settingRepository, ITrBookingDataSetBuilder bookingDataSetBuilder, IDbCommonRepository<TrDbRestaurantBooking> restaurantBookingRepository, IContentBuilder contentBuilder, IEmailUtil emailUtil)
        {
            _restaurantRepository = restaurantRepository;
            _dateTimeUtil = dateTimeUtil;
            _shopRepository = shopRepository;
            _shopContentRepository = shopContentRepository;
            _settingRepository = settingRepository;
            _bookingDataSetBuilder = bookingDataSetBuilder;
            _restaurantBookingRepository = restaurantBookingRepository;
            _contentBuilder = contentBuilder;
            _emailUtil = emailUtil;
        }

        public async Task<List<TrDbRestaurant>> GetRestaurantInfo(int shopId)
        {
            var restaurants = await _restaurantRepository.GetManyAsync(r => r.ShopId == shopId && r.IsActive.HasValue && r.IsActive.Value);
            return restaurants.ClearForOutPut();
        }

        public async Task<TrDbRestaurantBooking> RequestBooking(TrDbRestaurantBooking booking, int shopId)
        {
            Guard.NotNull(booking);
            Guard.AreEqual(booking.ShopId.Value, shopId);
            booking.Id = Guid.NewGuid().ToString();
            booking.Created = _dateTimeUtil.GetCurrentTime();

            var shopInfo =
                await _shopRepository.GetOneAsync(r => r.ShopId == shopId && r.IsActive.HasValue && r.IsActive.Value);
            if (shopInfo == null)
                throw new ServiceException("Cannot find shop info");
            var settings = await _settingRepository.GetManyAsync(r => r.ShopId == shopId);
            //1. Creating Booking record
            var newItem = await _restaurantBookingRepository.CreateAsync(booking);

            ////2. Getting Template for Shop and email to Wiiya
            //var content = await _shopContentRepository.GetOneAsync(r =>
            //    r.ShopId == shopId && r.Key == EmailTemplateEnum.BookingEmailTemplateTravelMealsShop.ToString());

            //var restaurant =
            //    await _restaurantRepository.GetOneAsync(r => r.Id == booking.RestaurantId && r.ShopId == shopId);
            //if (restaurant == null)
            //    throw new ServiceException("Cannot find shop info");

            //var dataset = _bookingDataSetBuilder.BuildTravelMealContent(shopInfo, restaurant, booking);
            //var bodyHtml = await _contentBuilder.BuildRazorContent(dataset, content.Content);

            ////2. Email Trello
            //var resultTrello = await _emailUtil.SendEmail(settings.ToList(), shopInfo.Email, "Travel Meals Booking",
            //    shopInfo.ContactEmail, "", "New Booking", null, bodyHtml, null);

            ////3. Email Client

            ////4. Email Shop

            return newItem;
        }
    }
}