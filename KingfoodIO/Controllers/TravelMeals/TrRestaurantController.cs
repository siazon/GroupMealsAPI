using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using App.Domain.Common;
using App.Domain.Common.Auth;
using App.Domain.Common.Customer;
using App.Domain.Config;
using App.Domain.TravelMeals;
using App.Domain.TravelMeals.Restaurant;
using App.Infrastructure.ServiceHandler.TravelMeals;
using App.Infrastructure.Utility.Common;
using KingfoodIO.Application.Filter;
using KingfoodIO.Common;
using KingfoodIO.Controllers.Common;
using KingfoodIO.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace KingfoodIO.Controllers.TravelMeals
{
    /// <summary>
    /// FoodCategory:  Chinese,Thai,Pizza,Burgers,Kebab,Chicken
    /// RestaurantTag: SpecialOffers,GoodStars,New,Halal
    /// MenuCalculateType:  DEFAULT, WesternFood, ChineseFood
    /// PaymentType: Full, Deposit, PayAtStore
    /// Features: Special offers, 4+ stars, New, Halal, Vegetarian(Key为0到4)
    /// Country: Ireland, UK, France (Key为string)
    /// PaymentMethod: Full, Percentage, Fixed(Key为0到2)
    /// </summary>
    [Route("api/[controller]/[action]")]
    public class TrRestaurantController : BaseController
    {
        private readonly ITrRestaurantServiceHandler _restaurantServiceHandler;

        IMemoryCache _memoryCache;
        ILogManager _logger;

        /// <summary>
        /// </summary>
        /// <param name="cachesettingConfig"></param>
        /// <param name="memoryCache"></param>
        /// <param name="redisCache"></param>
        /// <param name="restaurantBookingServiceHandler"></param>
        /// <param name="restaurantServiceHandler"></param>
        /// <param name="logger"></param>
        public TrRestaurantController(
            IOptions<CacheSettingConfig> cachesettingConfig, IMemoryCache memoryCache, IRedisCache redisCache, ITrRestaurantBookingServiceHandler restaurantBookingServiceHandler,
            ITrRestaurantServiceHandler restaurantServiceHandler, ILogManager logger) :
            base(cachesettingConfig, memoryCache, redisCache, logger)
        {
            _restaurantServiceHandler = restaurantServiceHandler;
            _logger = logger;
            _memoryCache = memoryCache;
        }

        /// <summary>
        /// </summary>
        /// <param name="Id"></param>
        /// <param name="shopId"></param>
        /// <param name="cache"></param>
        /// <returns>
        /// </returns>
        [HttpGet]
        //[ServiceFilter(typeof(AuthActionFilter))]
        [ProducesResponseType(typeof(List<TrDbRestaurant>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetRestaurant(string Id, int shopId, bool cache = false)
        {
            return await ExecuteAsync(shopId, cache, async () => await _restaurantServiceHandler.GetRestaurant(Id));
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="pageToken"></param>
        /// <param name="shopId"></param>
        /// <param name="country"></param>
        /// <param name="city"></param>
        /// <param name="content"></param>
        /// <param name="pageSize">-1时不分页</param>
        /// <param name="cache"></param>
        /// <returns></returns>
        [HttpPost]
        //[ServiceFilter(typeof(AuthActionFilter))]
        [ProducesResponseType(typeof(List<TrDbRestaurant>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetRestaurants([FromBody] string pageToken, int shopId, string country = "All", string city = "All", string content = "", int pageSize = -1, bool cache = true)
        {
            //return await ExecuteAsync(shopId, cache,
            //    async () => await _restaurantServiceHandler.GetRestaurantInfo(shopId));
            //string _city= HttpUtility.UrlDecode(city);

            DbToken userInfo = new DbToken();
            var authHeader = Request.Headers["Wauthtoken"];
            try
            {

                if (!string.IsNullOrEmpty(authHeader))
                    userInfo = new TokenEncryptorHelper().Decrypt<DbToken>(authHeader);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

            }
            return await ExecuteAsync(shopId, cache, async () => await _restaurantServiceHandler.GetRestaurants(shopId, country, city, content, userInfo, pageSize, pageToken));

        }

        [HttpPost]
        //[ServiceFilter(typeof(AuthActionFilter))]
        [ProducesResponseType(typeof(List<TrDbRestaurant>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetRestaurantsByAdmin([FromBody] string pageToken, int shopId, string country = "All", string city = "All", string content = "", int pageSize = -1, bool cache = true)
        {
            //return await ExecuteAsync(shopId, cache,
            //    async () => await _restaurantServiceHandler.GetRestaurantInfo(shopId));
            //string _city= HttpUtility.UrlDecode(city);

            DbToken userInfo = new DbToken();
            var authHeader = Request.Headers["Wauthtoken"];
            try
            {

                if (!string.IsNullOrEmpty(authHeader))
                    userInfo = new TokenEncryptorHelper().Decrypt<DbToken>(authHeader);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

            }
            return await ExecuteAsync(shopId, cache, async () => await _restaurantServiceHandler.GetRestaurantsByAdmin(shopId, country, city, content, userInfo, pageSize, pageToken));

        }

        [HttpGet]
        //[ServiceFilter(typeof(AuthActionFilter))]
        [ProducesResponseType(typeof(List<TrDbRestaurant>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetCitys(int shopId, bool cache = false)
        {
            return await ExecuteAsync(shopId, cache, async () => await _restaurantServiceHandler.GetCitys(shopId));
        }

        [HttpGet]
        //[ServiceFilter(typeof(AuthActionFilter))]
        [ProducesResponseType(typeof(List<TrDbRestaurant>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetCities(int shopId, bool cache = false)
        {

            return await ExecuteAsync(shopId, cache, async () => await _restaurantServiceHandler.GetCities(shopId));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="countries"></param>
        /// <param name="shopId"></param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(typeof(DbCustomer), (int)HttpStatusCode.OK)]
        [ServiceFilter(typeof(AuthActionFilter))]
        public async Task<IActionResult> UpsetCities([FromBody] DbCountry countries, int shopId)
        {
            return await ExecuteAsync(shopId, false,
                async () => await _restaurantServiceHandler.UpsetCities(countries, shopId));
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="restaurant"></param>
        /// <param name="shopId"></param>
        /// <returns></returns>
        [HttpPost]
        [Idempotent]
        [ProducesResponseType(typeof(DbCustomer), (int)HttpStatusCode.OK)]
        [ServiceFilter(typeof(AuthActionFilter))]
        public async Task<IActionResult> AddRestaurant([FromBody] TrDbRestaurant restaurant, int shopId)
        {
            return await ExecuteAsync(shopId, false,
                async () => await _restaurantServiceHandler.AddRestaurant(restaurant, shopId));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="restaurant"></param>
        /// <param name="shopId"></param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(typeof(DbCustomer), (int)HttpStatusCode.OK)]
        [ServiceFilter(typeof(AuthActionFilter))]
        public async Task<IActionResult> UpdateRestaurant([FromBody] TrDbRestaurant restaurant, int shopId)
        {
            return await ExecuteAsync(shopId, false,
                async () => await _restaurantServiceHandler.UpdateRestaurant(restaurant, shopId));
        }

        [ServiceFilter(typeof(AdminAuthFilter))]
        [HttpGet]
        [ProducesResponseType(typeof(DbCustomer), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> DeleteRestaurant(string id, string pwd, int shopId)
        {
            var authHeader = Request.Headers["Wauthtoken"];
            var userInfo = new TokenEncryptorHelper().Decrypt<DbToken>(authHeader);
            string email = userInfo.UserEmail;
            return await ExecuteAsync(shopId, false,
                async () => await _restaurantServiceHandler.DeleteRestaurant(id, email, pwd, shopId));
        }
    }
}