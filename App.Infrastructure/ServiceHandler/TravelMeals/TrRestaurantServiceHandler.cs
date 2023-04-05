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
        Task<List<TrDbRestaurant>> SearchRestaurantInfo(int shopId,string searchContent);

        Task<TrDbRestaurantBooking> RequestBooking(TrDbRestaurantBooking booking, int shopId);
        Task<TrDbRestaurant> AddRestaurant(TrDbRestaurant restaurant, int shopId);
        Task<List<TrDbRestaurantBooking>> SearchBookings(int shopId, string email);
    }

    public class TrRestaurantServiceHandler : ITrRestaurantServiceHandler
    {
        private readonly IDbCommonRepository<TrDbRestaurant> _restaurantRepository;
        private readonly IDateTimeUtil _dateTimeUtil;
        private readonly IDbCommonRepository<DbShop> _shopRepository;
        private readonly ITrBookingDataSetBuilder _bookingDataSetBuilder;
        private readonly IDbCommonRepository<TrDbRestaurantBooking> _restaurantBookingRepository;
        private readonly IContentBuilder _contentBuilder;
        private readonly IEmailUtil _emailUtil;

        public TrRestaurantServiceHandler(IDbCommonRepository<TrDbRestaurant> restaurantRepository, IDateTimeUtil dateTimeUtil, IDbCommonRepository<DbShop> shopRepository,  ITrBookingDataSetBuilder bookingDataSetBuilder, IDbCommonRepository<TrDbRestaurantBooking> restaurantBookingRepository, IContentBuilder contentBuilder, IEmailUtil emailUtil)
        {
            _restaurantRepository = restaurantRepository;
            _dateTimeUtil = dateTimeUtil;
            _shopRepository = shopRepository;
            _bookingDataSetBuilder = bookingDataSetBuilder;
            _restaurantBookingRepository = restaurantBookingRepository;
            _contentBuilder = contentBuilder;
            _emailUtil = emailUtil;
        }

        public async Task<List<TrDbRestaurant>> GetRestaurantInfo(int shopId)
        {
            var restaurants = await _restaurantRepository.GetManyAsync(r => r.ShopId == shopId && r.IsActive.HasValue && r.IsActive.Value);
            var temp = restaurants.ClearForOutPut();
            return temp;
        }
        public async Task<List<TrDbRestaurant>> SearchRestaurantInfo(int shopId,string searchContent)
        {
            var restaurants = await _restaurantRepository.GetManyAsync(r => r.ShopId == shopId 
            && (r.StoreName.Contains(searchContent)||r.StoreNameCn.Contains(searchContent)||r.ShopAddress.Contains(searchContent))
            && r.IsActive.HasValue && r.IsActive.Value);
            var temp = restaurants.ClearForOutPut();
            return temp;//TrDbRestaurant
        }
        public async Task<List<TrDbRestaurantBooking>> SearchBookings(int shopId, string email)
        {
            var Bookings = await _restaurantBookingRepository.GetManyAsync(r => r.UserEmail == email);
            return Bookings.ToList();
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
            //1. Creating Booking record
            var newItem = await _restaurantBookingRepository.CreateAsync(booking);

            //2. Getting Template for email
            var content = shopInfo.ShopContents.FirstOrDefault(a=>a.Key == EmailTemplateEnum.BookingEmailTemplateTravelMealsShop.ToString());
            var temp = content.Content;
            var restaurant =
                await _restaurantRepository.GetOneAsync(r => r.Id == booking.RestaurantId && r.ShopId == shopId);
            if (restaurant == null)
                throw new ServiceException("Cannot find shop info");

            var dataset = _bookingDataSetBuilder.BuildTravelMealContent( restaurant, booking);
            var bodyHtml = await _contentBuilder.BuildRazorContent(dataset, content.Content);
            //2. Email Trello
            var resultTrello = await _emailUtil.SendEmail(shopInfo.ShopSettings, shopInfo.Email, "Travel Meals Booking",
                shopInfo.ContactEmail, "", "New Group Meals Booking", null, bodyHtml, null);
            //3. Email Client
            var resultClient = await _emailUtil.SendEmail(shopInfo.ShopSettings, shopInfo.Email, "Travel Meals Booking",
                booking.UserEmail, "", "New Group Meals Booking", null, bodyHtml, null);


            return newItem;
        }
        public async Task<TrDbRestaurant> AddRestaurant(TrDbRestaurant restaurant, int shopId)
        {
            Guard.NotNull(restaurant);
            var existingRestaurant =
               await _restaurantRepository.GetOneAsync(r => r.ShopId == shopId && r.StoreName == restaurant.StoreName&&r.ShopAddress== restaurant.ShopAddress);
            if (existingRestaurant != null)
                throw new ServiceException("Customer Already Exists");
            var newItem = restaurant.Clone();

            newItem.ShopId = shopId;
            newItem.Created = _dateTimeUtil.GetCurrentTime();
            newItem.Updated = _dateTimeUtil.GetCurrentTime();
            newItem.IsActive = true;

            var savedRestaurant = await _restaurantRepository.CreateAsync(newItem);
            return savedRestaurant;
        }
    }
}