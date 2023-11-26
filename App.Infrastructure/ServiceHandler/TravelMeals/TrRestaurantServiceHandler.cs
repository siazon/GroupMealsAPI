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
using System.Diagnostics;
using App.Domain.Common.Stripe;
using Stripe.Issuing;
using Microsoft.AspNetCore.Http;
using App.Domain.Holiday;
using App.Domain.Common;

namespace App.Infrastructure.ServiceHandler.TravelMeals
{
    public interface ITrRestaurantServiceHandler
    {
        Task<List<TrDbRestaurant>> GetRestaurantInfo(int shopId);
        Task<KeyValuePair<string, List<TrDbRestaurant>>> GetRestaurantInfo(int shopId,  int pageSize = -1, string continuationToke = null);
        Task<List<TrDbRestaurant>> SearchRestaurantInfo(int shopId, string searchContent);

        Task<TrDbRestaurantBooking> RequestBooking(TrDbRestaurantBooking booking, int shopId );
        Task<TrDbRestaurant> AddRestaurant(TrDbRestaurant restaurant, int shopId);
        Task<List<TrDbRestaurantBooking>> SearchBookings(int shopId, string email, string content);
      
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
        private ITrRestaurantBookingServiceHandler _trRestaurantBookingServiceHandler;
        ILogManager _logger;

        public TrRestaurantServiceHandler(IDbCommonRepository<TrDbRestaurant> restaurantRepository, IDateTimeUtil dateTimeUtil, ILogManager logger, IDbCommonRepository<DbShop> shopRepository, ITrBookingDataSetBuilder bookingDataSetBuilder, IDbCommonRepository<TrDbRestaurantBooking> restaurantBookingRepository, ITrRestaurantBookingServiceHandler trRestaurantBookingServiceHandler, IContentBuilder contentBuilder, IEmailUtil emailUtil)
        {
            _restaurantRepository = restaurantRepository;
            _dateTimeUtil = dateTimeUtil;
            _shopRepository = shopRepository;
            _bookingDataSetBuilder = bookingDataSetBuilder;
            _restaurantBookingRepository = restaurantBookingRepository;
            _contentBuilder = contentBuilder;
            _emailUtil = emailUtil;
            _trRestaurantBookingServiceHandler = trRestaurantBookingServiceHandler;
            _logger= logger;
        }
        public async Task<KeyValuePair<string, List<TrDbRestaurant>>> GetRestaurantInfo(int shopId,  int pageSize = -1, string continuationToke = null)
        {

                DateTime stime = DateTime.Now;
                KeyValuePair<string, IEnumerable<TrDbRestaurant>> currentPage = await _restaurantRepository
                    .GetManyAsync(r => r.ShopId == shopId && r.IsActive.HasValue && r.IsActive.Value, pageSize, continuationToke);
                var temp = currentPage.Value.ClearForOutPut().OrderBy(a => a.SortOrder).ToList();
                continuationToke = currentPage.Key;
                _logger.LogInfo("_restaurantRepository.GetManyAsync:" + (DateTime.Now - stime).ToString());

                return new KeyValuePair<string, List<TrDbRestaurant>>(currentPage.Key, temp);
        }
        public async Task<List<TrDbRestaurant>> GetRestaurantInfo(int shopId)
        {
            DateTime stime = DateTime.Now;
            var restaurants = await _restaurantRepository.GetManyAsync(r => r.ShopId == shopId && r.IsActive.HasValue && r.IsActive.Value);
            var temp = restaurants.ClearForOutPut().OrderBy(a => a.SortOrder).ToList();
            _logger.LogInfo("_restaurantRepository.GetManyAsync:" + (DateTime.Now - stime).ToString());
            Console.WriteLine("_restaurantRepository.GetManyAsync:" + (DateTime.Now - stime).ToString());
            return temp;
        }
        public async Task<List<TrDbRestaurant>> SearchRestaurantInfo(int shopId, string searchContent)
        {
            var restaurants = await _restaurantRepository.GetManyAsync(r => r.ShopId == shopId
            && (r.StoreName.Contains(searchContent) || r.StoreNameCn.Contains(searchContent) || r.ShopAddress.Contains(searchContent))
            && r.IsActive.HasValue && r.IsActive.Value);
            var temp = restaurants.ClearForOutPut();
            return temp;//TrDbRestaurant
        }
        public async Task<List<TrDbRestaurantBooking>> SearchBookings(int shopId, string email, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                var Bookings = await _restaurantBookingRepository.GetManyAsync(r => 1==1);
                var list = Bookings.ToList().Where(a=>a.CustomerEmail==email|| a.Details.Any(b=>b.RestaurantEmail==email)).Select(c=>c).ToList();
                return list;
            }
            else
            {
                var Bookings = await _restaurantBookingRepository.GetManyAsync(r =>
                (r.CustomerEmail == email||r.Details.Any(b=>b.RestaurantEmail==email) )&& r.Details[0].RestaurantName.Contains(content));
                var list = Bookings.ToList();
              
                return list;
            }
        }
        public async Task<TrDbRestaurantBooking> RequestBooking(TrDbRestaurantBooking booking, int shopId )
        {
            _logger.LogInfo("RequestBooking" + shopId);
            Guard.NotNull(booking);
            Guard.AreEqual(booking.ShopId.Value, shopId);
            //var findRestaurant = await _restaurantRepository.GetOneAsync(r => r.ShopId == shopId && r.Id == booking.RestaurantId);
            //if (findRestaurant == null)
            //    throw new ServiceException("Cannot Find tour");
            TourBooking newBooking;
            var createTime = _dateTimeUtil.GetCurrentTime();
            var exsitBookings = await _restaurantBookingRepository.GetManyAsync(r => r.Status == OrderStatusEnum.None && r.CustomerEmail == booking.CustomerEmail);
            var exsitBooking = exsitBookings.FirstOrDefault(a => (createTime - a.Created).Value.Hours < 2);
            if (exsitBooking != null)
            {
                exsitBooking.Created = _dateTimeUtil.GetCurrentTime();
                exsitBooking.Details = booking.Details;
                exsitBooking.CustomerName = booking.CustomerName;
                exsitBooking.CustomerPhone= booking.CustomerPhone;
                var savedBooking = await _restaurantBookingRepository.UpdateAsync(exsitBooking);

                return savedBooking;
            }
            else
            {
                booking.Id = Guid.NewGuid().ToString();
                booking.Created = _dateTimeUtil.GetCurrentTime();
                var newItem = await _restaurantBookingRepository.CreateAsync(booking);
                return newItem;
            }
        }
        public async Task<TrDbRestaurant> AddRestaurant(TrDbRestaurant restaurant, int shopId)
        {
            Guard.NotNull(restaurant);
            var existingRestaurant =
               await _restaurantRepository.GetOneAsync(r => r.ShopId == shopId && r.StoreName == restaurant.StoreName && r.ShopAddress == restaurant.ShopAddress);
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