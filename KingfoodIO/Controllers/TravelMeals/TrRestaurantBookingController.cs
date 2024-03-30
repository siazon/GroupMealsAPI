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
    public class TrRestaurantBookingController : BaseController
    {
        private readonly ITrRestaurantBookingServiceHandler _restaurantBookingServiceHandler;

        IMemoryCache _memoryCache;
        ILogManager _logger;
        public TrRestaurantBookingController(
            IOptions<CacheSettingConfig> cachesettingConfig, IMemoryCache memoryCache, IRedisCache redisCache, ITrRestaurantBookingServiceHandler restaurantBookingServiceHandler,
            ITrRestaurantServiceHandler restaurantServiceHandler, ILogManager logger) :
            base(cachesettingConfig, memoryCache, redisCache, logger)
        {
            _logger = logger;
            _memoryCache = memoryCache;
            _restaurantBookingServiceHandler = restaurantBookingServiceHandler;
        }

      
        [HttpPost]
        [ProducesResponseType(typeof(TrDbRestaurantBooking), (int)HttpStatusCode.OK)]
        [ServiceFilter(typeof(AuthActionFilter))]
        public async Task<IActionResult> RequestTravelMealsBooking([FromBody] TrDbRestaurantBooking booking, int shopId)
        {
            var authHeader = Request.Headers["Wauthtoken"];
            var temp = new TokenEncryptorHelper().Decrypt<DbToken>(authHeader);
            return await ExecuteAsync(shopId, false,
                async () => await _restaurantBookingServiceHandler.RequestBooking(booking, shopId, temp.UserEmail));
        }

        [HttpPost]
        [ProducesResponseType(typeof(TrDbRestaurantBooking), (int)HttpStatusCode.OK)]
        [ServiceFilter(typeof(AuthActionFilter))]
        public async Task<IActionResult> ModifyBooking([FromBody] TrDbRestaurantBooking booking, int shopId)
        {
            var authHeader = Request.Headers["Wauthtoken"];
            var temp = new TokenEncryptorHelper().Decrypt<DbToken>(authHeader);
            return await ExecuteAsync(shopId, false,
                async () => await _restaurantBookingServiceHandler.ModifyBooking(booking, shopId, temp.UserEmail));
        }

        [HttpGet]
        [ServiceFilter(typeof(AuthActionFilter))]
        [ProducesResponseType(typeof(List<TrDbRestaurant>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> CancelBooking(int shopId, string bookingId, string detailId, bool cache = false)
        {
            return await ExecuteAsync(shopId, cache,
                async () => await _restaurantBookingServiceHandler.CancelBooking(bookingId, detailId, shopId));
        }
        [HttpGet]
        [ServiceFilter(typeof(AuthActionFilter))]
        [ProducesResponseType(typeof(List<TrDbRestaurant>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> DeleteBooking(int shopId, string bookingId, bool cache = false)
        {
            return await ExecuteAsync(shopId, cache,
                async () => await _restaurantBookingServiceHandler.DeleteBooking(bookingId, shopId));
        }

        [HttpPost]
        [ServiceFilter(typeof(AuthActionFilter))]
        [ProducesResponseType(typeof(List<TrDbRestaurant>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> SearchBookings([FromBody] string pageToken, int shopId, string email, string content, int pageSize = -1, bool cache = true)
        {

            return await ExecuteAsync(shopId, cache,
                async () => await _restaurantBookingServiceHandler.SearchBookings(shopId, email, content, pageSize, pageToken));
        }

        [HttpPost]
        [ServiceFilter(typeof(AuthActionFilter))]
        [ProducesResponseType(typeof(List<TrDbRestaurant>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> SearchBookingsByRestaurant([FromBody] string pageToken, int shopId, string email, string content, int pageSize = -1, bool cache = true)
        {

            return await ExecuteAsync(shopId, cache,
                async () => await _restaurantBookingServiceHandler.SearchBookingsByRestaurant(shopId, email, content, pageSize, pageToken));
        }



        [HttpGet]
        [ServiceFilter(typeof(AuthActionFilter))]
        [ProducesResponseType(typeof(List<TrDbRestaurant>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> ResendEmail(int shopId, string bookingId, bool cache = false)
        {
            return await ExecuteAsync(shopId, cache,
                async () => await _restaurantBookingServiceHandler.ResendEmail(bookingId));
        }
        [HttpGet]
        //[ServiceFilter(typeof(AuthActionFilter))]
        [ProducesResponseType(typeof(List<TrDbRestaurant>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> UpdateAccepted(int shopId, string bookingId, string subId, string e, int acceptType, bool cache = false)
        {
            //var authHeader = Request.Headers["Wauthtoken"];
            //var temp = new TokenEncryptorHelper().Decrypt<DbToken>(authHeader);
            return await ExecuteAsync(shopId, cache,
                async () => await _restaurantBookingServiceHandler.UpdateAccepted(bookingId, subId, acceptType, e));
        }
        [HttpPost]
        //[ServiceFilter(typeof(AuthActionFilter))]
        [ProducesResponseType(typeof(bool), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> UpdateAcceptedReason([FromBody] string reason, int shopId, string bookingId, string subId, string e, bool cache = false)
        {
            //var authHeader = Request.Headers["Wauthtoken"];
            //var temp = new TokenEncryptorHelper().Decrypt<DbToken>(authHeader);
            return await ExecuteAsync(shopId, cache,
                async () => await _restaurantBookingServiceHandler.UpdateAcceptedReason(bookingId, subId, reason, e));
        }
    }
}