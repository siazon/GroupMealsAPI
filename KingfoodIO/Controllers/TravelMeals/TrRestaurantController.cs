using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
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

        public TrRestaurantController(
            IOptions<CacheSettingConfig> cachesettingConfig, IRedisCache redisCache, ITrRestaurantServiceHandler restaurantServiceHandler, ILogManager logger) : base(cachesettingConfig, redisCache, logger)
        {
            _restaurantServiceHandler = restaurantServiceHandler;
        }

        [HttpGet]
        [ServiceFilter(typeof(AuthActionFilter))]
        [ProducesResponseType(typeof(List<TrDbRestaurant>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetRestaurants(int shopId, bool cache = true)
        {
            return await ExecuteAsync(shopId, cache,
                async () => await _restaurantServiceHandler.GetRestaurantInfo(shopId));
        }

        [HttpPost]
        [ProducesResponseType(typeof(TrDbRestaurantBooking), (int)HttpStatusCode.OK)]
        [ServiceFilter(typeof(AuthActionFilter))]
        public async Task<IActionResult> RequestTravelMealsBooking([FromBody] TrDbRestaurantBooking booking, int shopId)
        {
            return await ExecuteAsync(shopId, false,
                async () => await _restaurantServiceHandler.RequestBooking(booking, shopId));
        }
    }
}