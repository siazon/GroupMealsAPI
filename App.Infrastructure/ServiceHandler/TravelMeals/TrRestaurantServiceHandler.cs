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
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Quartz.Impl;
using Quartz;
using App.Domain.Common.Auth;
using Newtonsoft.Json.Linq;
using Microsoft.Azure.Cosmos.Serialization.HybridRow.Schemas;
using Microsoft.Extensions.Caching.Memory;

namespace App.Infrastructure.ServiceHandler.TravelMeals
{
    public interface ITrRestaurantServiceHandler
    {
        Task<ResponseModel> GetRestaurantInfo(int shopId, string country, int pageSize = -1, string continuationToke = null);


        Task<ResponseModel> GetRestaurant(string Id);
        Task<TrDbRestaurant> AddRestaurant(TrDbRestaurant restaurant, int shopId);
        Task<TrDbRestaurant> UpdateRestaurant(TrDbRestaurant restaurant, int shopId);

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
        ITwilioUtil _twilioUtil;
        IMemoryCache _memoryCache;

        Microsoft.AspNetCore.Hosting.IHostingEnvironment _environment;
        private ITrRestaurantBookingServiceHandler _trRestaurantBookingServiceHandler;
        ILogManager _logger;

        public TrRestaurantServiceHandler(IDbCommonRepository<TrDbRestaurant> restaurantRepository, IDateTimeUtil dateTimeUtil, ILogManager logger, ITwilioUtil twilioUtil,
            Microsoft.AspNetCore.Hosting.IHostingEnvironment environment, IDbCommonRepository<DbShop> shopRepository, ITrBookingDataSetBuilder bookingDataSetBuilder,
            IDbCommonRepository<TrDbRestaurantBooking> restaurantBookingRepository, ITrRestaurantBookingServiceHandler trRestaurantBookingServiceHandler, IMemoryCache memoryCache,
            IContentBuilder contentBuilder, IEmailUtil emailUtil)
        {
            _restaurantRepository = restaurantRepository;
            _dateTimeUtil = dateTimeUtil;
            _shopRepository = shopRepository;
            _bookingDataSetBuilder = bookingDataSetBuilder;
            _restaurantBookingRepository = restaurantBookingRepository;
            _contentBuilder = contentBuilder;
            _emailUtil = emailUtil;
            _environment = environment;
            _twilioUtil = twilioUtil;
            _trRestaurantBookingServiceHandler = trRestaurantBookingServiceHandler;
            _logger = logger;
            _memoryCache = memoryCache;
        }
        public async Task<ResponseModel> GetRestaurantInfo(int shopId, string country, int pageSize = -1, string continuationToke = null)
        {
            DateTime stime = DateTime.Now;
            KeyValuePair<string, IEnumerable<TrDbRestaurant>> currentPage;
            //for (int i = 0; i < 5; i++)
            if (country == "All")
            {
                stime = DateTime.Now;
                currentPage = await _restaurantRepository
                   .GetManyAsync(r => r.ShopId == shopId && r.IsActive.HasValue && r.IsActive.Value == true, pageSize, continuationToke);
                Console.WriteLine(country+DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " : " + (DateTime.Now - stime).TotalMilliseconds);
                _logger.LogInfo(country + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " : " + (DateTime.Now - stime).TotalMilliseconds);
            }
            else
            {
                stime = DateTime.Now;
                currentPage = await _restaurantRepository
                    .GetManyAsync(r => r.ShopId == shopId && r.Country == country && r.IsActive.HasValue && r.IsActive.Value, pageSize, continuationToke);
                Console.WriteLine(country + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " : " + (DateTime.Now - stime).TotalMilliseconds);
                _logger.LogInfo(country + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " : " + (DateTime.Now - stime).TotalMilliseconds);
            }
                var temp = currentPage.Value.ClearForOutPut().OrderByDescending(a => a.Created).OrderBy(a => a.SortOrder).ToList();
            continuationToke = currentPage.Key;
            var time = (DateTime.Now - stime).ToString();
            _logger.LogInfo("_restaurantRepository.GetManyAsync:" + (DateTime.Now - stime).ToString());

            return new ResponseModel { msg = "ok", code = 200, token = continuationToke, data = temp };
        }

        public async Task<ResponseModel> GetRestaurant(string Id)
        {
            var existingRestaurant =
               await _restaurantRepository.GetOneAsync(r => r.Id == Id);
            if (existingRestaurant != null)
                return new ResponseModel { msg = "ok", code = 200, data = existingRestaurant };
            else
                return new ResponseModel { msg = "Restaurant Already Exists", code = 501, data = existingRestaurant };

        }



        public async Task<TrDbRestaurant> AddRestaurant(TrDbRestaurant restaurant, int shopId)
        {
            Guard.NotNull(restaurant);
            var existingRestaurant =
               await _restaurantRepository.GetOneAsync(r => r.ShopId == shopId && r.StoreName == restaurant.StoreName && r.Address == restaurant.Address);
            if (existingRestaurant != null)
                throw new ServiceException("Restaurant Already Exists");

            restaurant.Id = Guid.NewGuid().ToString();
            restaurant.ShopId = shopId;
            restaurant.Created = _dateTimeUtil.GetCurrentTime();
            restaurant.Updated = _dateTimeUtil.GetCurrentTime();
            restaurant.IsActive = true;

            var savedRestaurant = await _restaurantRepository.CreateAsync(restaurant);
            return savedRestaurant;
        }
        public async Task<TrDbRestaurant> UpdateRestaurant(TrDbRestaurant restaurant, int shopId)
        {
            Guard.NotNull(restaurant);
            var existingRestaurant =
               await _restaurantRepository.GetOneAsync(r => r.ShopId == shopId && r.StoreName == restaurant.StoreName && r.Address == restaurant.Address);
            if (existingRestaurant == null)
                throw new ServiceException("Restaurant Not Exists");
            restaurant.Updated = _dateTimeUtil.GetCurrentTime();

            var savedRestaurant = await _restaurantRepository.UpdateAsync(restaurant);
            return savedRestaurant;
        }

    }
}