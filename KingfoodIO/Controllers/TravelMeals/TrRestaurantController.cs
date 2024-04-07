using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using App.Domain.Common;
using App.Domain.Common.Auth;
using App.Domain.Common.Customer;
using App.Domain.Config;
using App.Domain.TravelMeals;
using App.Domain.TravelMeals.Restaurant;
using App.Infrastructure.ServiceHandler.TravelMeals;
using App.Infrastructure.Utility.Common;
using KingfoodIO.Application.Filter;
using KingfoodIO.Controllers.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace KingfoodIO.Controllers.TravelMeals
{
    [Route("api/[controller]/[action]")]
    public class TrRestaurantController : BaseController
    {
        private readonly ITrRestaurantServiceHandler _restaurantServiceHandler;

        IMemoryCache _memoryCache;
        ILogManager _logger;
        public TrRestaurantController(
            IOptions<CacheSettingConfig> cachesettingConfig, IMemoryCache memoryCache, IRedisCache redisCache, ITrRestaurantBookingServiceHandler restaurantBookingServiceHandler,
            ITrRestaurantServiceHandler restaurantServiceHandler, ILogManager logger) :
            base(cachesettingConfig, memoryCache, redisCache, logger)
        {
            _restaurantServiceHandler = restaurantServiceHandler;
            _logger = logger;
            _memoryCache = memoryCache;
        }

        [HttpGet]
        //[ServiceFilter(typeof(AuthActionFilter))]
        [ProducesResponseType(typeof(List<TrDbRestaurant>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetRestaurant(string Id, int shopId,  bool cache = true)
        {
            return await ExecuteAsync(shopId, cache, async () => await _restaurantServiceHandler.GetRestaurant(Id));
        }
        [HttpPost]
        //[ServiceFilter(typeof(AuthActionFilter))]
        [ProducesResponseType(typeof(List<TrDbRestaurant>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetRestaurants([FromBody] string pageToken, int shopId,string country, int pageSize = -1, bool cache = true)
        {
            //return await ExecuteAsync(shopId, cache,
            //    async () => await _restaurantServiceHandler.GetRestaurantInfo(shopId));

            return await ExecuteAsync(shopId, cache, async () => await _restaurantServiceHandler.GetRestaurantInfo(shopId,country, pageSize, pageToken));
        }

        [HttpPost]
        [ProducesResponseType(typeof(DbCustomer), (int)HttpStatusCode.OK)]
        [ServiceFilter(typeof(AuthActionFilter))]
        public async Task<IActionResult> AddRestaurant([FromBody] TrDbRestaurant restaurant, int shopId)
        {
            return await ExecuteAsync(shopId, false,
                async () => await _restaurantServiceHandler.AddRestaurant(restaurant, shopId));
        }

        [HttpPost]
        [ProducesResponseType(typeof(DbCustomer), (int)HttpStatusCode.OK)]
        [ServiceFilter(typeof(AuthActionFilter))]
        public async Task<IActionResult> UpdateRestaurant([FromBody] TrDbRestaurant restaurant, int shopId)
        {
            return await ExecuteAsync(shopId, false,
                async () => await _restaurantServiceHandler.UpdateRestaurant(restaurant, shopId));
        }
     
      
    }
}