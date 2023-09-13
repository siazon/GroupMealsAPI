using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using App.Domain.Common.Customer;
using App.Domain.Config;
using App.Domain.TravelMeals;
using App.Domain.TravelMeals.Restaurant;
using App.Infrastructure.ServiceHandler.TravelMeals;
using App.Infrastructure.Utility.Common;
using KingfoodIO.Application.Filter;
using KingfoodIO.Controllers.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace KingfoodIO.Controllers.TravelMeals
{
    [Route("api/[controller]/[action]")]
    public class TrRestaurantController : BaseController
    {
        private readonly ITrRestaurantServiceHandler _restaurantServiceHandler;
        private readonly ITrRestaurantBookingServiceHandler _restaurantBookingServiceHandler;
        ILogManager _logger;
        public TrRestaurantController(
            IOptions<CacheSettingConfig> cachesettingConfig, IRedisCache redisCache, ITrRestaurantBookingServiceHandler restaurantBookingServiceHandler, ITrRestaurantServiceHandler restaurantServiceHandler, ILogManager logger) : base(cachesettingConfig, redisCache, logger)
        {
            _restaurantServiceHandler = restaurantServiceHandler;
            _logger = logger;
            _restaurantBookingServiceHandler= restaurantBookingServiceHandler;
        }

        [HttpGet]
        [ServiceFilter(typeof(AuthActionFilter))]
        [ProducesResponseType(typeof(List<TrDbRestaurant>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetRestaurants(int shopId, bool cache = true)
        {
            return await ExecuteAsync(shopId, cache,
                async () => await _restaurantServiceHandler.GetRestaurantInfo(shopId));
        }
        [HttpGet]
        [ServiceFilter(typeof(AuthActionFilter))]
        [ProducesResponseType(typeof(List<TrDbRestaurant>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> SearchRestaurants(int shopId,string searchContent, bool cache = true)
        {
            return await ExecuteAsync(shopId, cache,
                async () => await _restaurantServiceHandler.SearchRestaurantInfo(shopId, searchContent));
        }
        [HttpPost]
        [ProducesResponseType(typeof(TrDbRestaurantBooking), (int)HttpStatusCode.OK)]
        [ServiceFilter(typeof(AuthActionFilter))]
        public async Task<TrDbRestaurantBooking> RequestTravelMealsBooking([FromBody] TrDbRestaurantBooking booking, int shopId)
        {
            return await  _restaurantServiceHandler.RequestBooking(booking, shopId);
        }
        [HttpPost]
        [ProducesResponseType(typeof(DbCustomer), (int)HttpStatusCode.OK)]
        [ServiceFilter(typeof(AuthActionFilter))]
        public async Task<IActionResult> AddRestaurant([FromBody] TrDbRestaurant restaurant, int shopId)
        {
            return await ExecuteAsync(shopId, false,
                async () => await _restaurantServiceHandler.AddRestaurant(restaurant, shopId));
        }
        [HttpGet]
        [ServiceFilter(typeof(AuthActionFilter))]
        [ProducesResponseType(typeof(List<TrDbRestaurant>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> SearchBookings(int shopId, string email,string content, bool cache = true)
        {
            return await ExecuteAsync(shopId, cache,
                async () => await _restaurantServiceHandler.SearchBookings(shopId, email, content));
        }
        [HttpGet]
        [ServiceFilter(typeof(AuthActionFilter))]
        [ProducesResponseType(typeof(List<TrDbRestaurant>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> ResendEmail(int shopId, string bookingId,  bool cache = true)
        {
            return await ExecuteAsync(shopId, cache,
                async () => await _restaurantBookingServiceHandler.ResendEmail(bookingId));
        }
        [HttpGet]
        [ServiceFilter(typeof(AuthActionFilter))]
        [ProducesResponseType(typeof(List<TrDbRestaurant>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> UpdateAccepted(int shopId, string bookingId,int acceptType, bool cache = true)
        {
            return await ExecuteAsync(shopId, cache,
                async () => await _restaurantBookingServiceHandler.UpdateAccepted(bookingId, acceptType));
        }
        [HttpPost]
        [ServiceFilter(typeof(AuthActionFilter))]
        [ProducesResponseType(typeof(bool), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> UpdateAcceptedReason([FromBody] string reason, int shopId, string bookingId, bool cache = true)
        {
            return await ExecuteAsync(shopId, cache,
                async () => await _restaurantBookingServiceHandler.UpdateAcceptedReason(bookingId, reason));
        }
    }
}