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
using App.Infrastructure.ServiceHandler.Common;
using App.Infrastructure.ServiceHandler.TravelMeals;
using App.Infrastructure.Utility.Common;
using KingfoodIO.Application.Filter;
using KingfoodIO.Controllers.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualBasic;

namespace KingfoodIO.Controllers.TravelMeals
{
    /// <summary>
    /// Status: None=0,UnAccepted=1,Accepted=2,Canceled=3,OpenOrder = 4, Settled=5,
    /// AcceptStatus: 0:Defult, 1:Accepted, 2:Declined
    /// </summary>
    [Route("api/[controller]/[action]")]
    public class TrRestaurantBookingController : BaseController
    {
        private readonly ITrRestaurantBookingServiceHandler _restaurantBookingServiceHandler;

        IMemoryCache _memoryCache;
        ILogManager _logger;
        IAmountCaculaterUtil _caculaterUtil;
        private readonly IShopServiceHandler _shopServiceHandler;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cachesettingConfig"></param>
        /// <param name="memoryCache"></param>
        /// <param name="redisCache"></param>
        /// <param name="restaurantBookingServiceHandler"></param>
        /// <param name="restaurantServiceHandler"></param>
        /// <param name="logger"></param>
        public TrRestaurantBookingController(
            IOptions<CacheSettingConfig> cachesettingConfig, IMemoryCache memoryCache, IRedisCache redisCache, ITrRestaurantBookingServiceHandler restaurantBookingServiceHandler,
            ITrRestaurantServiceHandler restaurantServiceHandler, IAmountCaculaterUtil caculaterUtil, IShopServiceHandler shopServiceHandler, ILogManager logger) :
            base(cachesettingConfig, memoryCache, redisCache, logger)
        {
            _logger = logger;
            _memoryCache = memoryCache;
            _restaurantBookingServiceHandler = restaurantBookingServiceHandler;
            _caculaterUtil= caculaterUtil;
            _shopServiceHandler= shopServiceHandler;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="booking"></param>
        /// <param name="shopId"></param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(typeof(TrDbRestaurantBooking), (int)HttpStatusCode.OK)]
        [ServiceFilter(typeof(AuthActionFilter))]
        public async Task<IActionResult> RequestTravelMealsBooking([FromBody] TrDbRestaurantBooking booking, int shopId)
        {
            var authHeader = Request.Headers["Wauthtoken"];
            var temp = new TokenEncryptorHelper().Decrypt<DbToken>(authHeader);
            return await ExecuteAsync(shopId, false,
                async () => await _restaurantBookingServiceHandler.RequestBooking(booking, shopId, temp.UserId));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="booking"></param>
        /// <param name="shopId"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="shopId"></param>
        /// <param name="bookingId"></param>
        /// <param name="detailId"></param>
        /// <param name="cache"></param>
        /// <returns></returns>
        [HttpGet]
        [ServiceFilter(typeof(AuthActionFilter))]
        [ProducesResponseType(typeof(List<TrDbRestaurant>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> CancelBooking(int shopId, string bookingId, string detailId, bool cache = false)
        {

            var authHeader = Request.Headers["Wauthtoken"];
            var temp = new TokenEncryptorHelper().Decrypt<DbToken>(authHeader);
            return await ExecuteAsync(shopId, cache,
                async () => await _restaurantBookingServiceHandler.CancelBooking(bookingId, detailId, shopId, temp.UserEmail));
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="shopId"></param>
        /// <param name="bookingId"></param>
        /// <param name="cache"></param>
        /// <returns></returns>
        [HttpGet]
        [ServiceFilter(typeof(AuthActionFilter))]
        [ProducesResponseType(typeof(List<TrDbRestaurant>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> DeleteBooking(int shopId, string bookingId, bool cache = false)
        {
            return await ExecuteAsync(shopId, cache,
                async () => await _restaurantBookingServiceHandler.DeleteBooking(bookingId, shopId));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pageToken"></param>
        /// <param name="shopId"></param>
        /// <param name="email">TA的邮箱</param>
        /// <param name="content"></param>
        /// <param name="pageSize"></param>
        /// <param name="cache"></param>
        /// <returns></returns>
        [HttpPost]
        [ServiceFilter(typeof(AuthActionFilter))]
        [ProducesResponseType(typeof(List<TrDbRestaurant>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> SearchBookings([FromBody] string pageToken, int shopId, string email, string content, int pageSize = -1, bool cache = true)
        {

            return await ExecuteAsync(shopId, cache,
                async () => await _restaurantBookingServiceHandler.SearchBookings(shopId, email, content, pageSize, pageToken));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pageToken"></param>
        /// <param name="shopId"></param>
        /// <param name="email">餐厅的邮箱</param>
        /// <param name="content"></param>
        /// <param name="pageSize"></param>
        /// <param name="cache"></param>
        /// <returns></returns>
        [HttpPost]
        [ServiceFilter(typeof(AuthActionFilter))]
        [ProducesResponseType(typeof(List<TrDbRestaurant>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> SearchBookingsByRestaurant([FromBody] string pageToken, int shopId, string email, string content, int pageSize = -1, bool cache = true)
        {

            return await ExecuteAsync(shopId, cache,
                async () => await _restaurantBookingServiceHandler.SearchBookingsByRestaurant(shopId, email, content, pageSize, pageToken));
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="shopId"></param>
        /// <param name="bookingId"></param>
        /// <param name="cache"></param>
        /// <returns></returns>
        [HttpGet]
        [ServiceFilter(typeof(AuthActionFilter))]
        [ProducesResponseType(typeof(List<TrDbRestaurant>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> ResendEmail(int shopId, string bookingId, bool cache = false)
        {
    
            return await ExecuteAsync(shopId, cache,
                async () => await _restaurantBookingServiceHandler.ResendEmail(bookingId));
        }


      /// <summary>
      /// 
      /// </summary>
      /// <param name="shopId"></param>
      /// <param name="isModify"></param>
      /// <param name="detailId"></param>
      /// <param name="cache"></param>
      /// <returns></returns>
        [HttpGet]
        [ServiceFilter(typeof(AuthActionFilter))]
        [ProducesResponseType(typeof(List<TrDbRestaurant>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> CalculateBookingItemAmount(int shopId,bool isModify, string detailId, bool cache = false)
        {
            var authHeader = Request.Headers["Wauthtoken"];
            var user = new TokenEncryptorHelper().Decrypt<DbToken>(authHeader);
            return await ExecuteAsync(shopId, cache,
                async () => await _restaurantBookingServiceHandler.GetBookingItemAmount(isModify, user.UserId,detailId));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cartInfoIds"></param>
        /// <param name="shopId"></param>
        /// <param name="isModify">是否订单修改</param>
        /// <param name="currency">isModify=false时必填</param>
        /// <param name="cache"></param>
        /// <returns></returns>
        [HttpPost]
        [ServiceFilter(typeof(AuthActionFilter))]
        [ProducesResponseType(typeof(List<TrDbRestaurant>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> CalculateBookingAmount([FromBody]List<string> cartInfoIds,  int shopId, bool isModify,string currency, bool cache = false)
        {
            DateTime sdate = DateTime.Now;
            var authHeader = Request.Headers["Wauthtoken"];
            var user = new TokenEncryptorHelper().Decrypt<DbToken>(authHeader);
            double rate = 1;
            if (!isModify)
            {
                var res = await _shopServiceHandler.GetExchangeRate(shopId);
                rate= res.Rate;
            }
            Console.WriteLine("controller: " + (DateTime.Now - sdate).TotalMilliseconds);
            return await ExecuteAsync(shopId, cache,
                async () => await _restaurantBookingServiceHandler.GetBookingAmount(isModify, currency, user.UserId,rate, cartInfoIds));
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="shopId"></param>
        /// <param name="bookingId"></param>
        /// <param name="subId">detailsId</param>
        /// <param name="e">email</param>
        /// <param name="acceptType">1：接收，2拒绝</param>
        /// <param name="cache"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reason">string</param>
        /// <param name="shopId"></param>
        /// <param name="bookingId"></param>
        /// <param name="subId">detailsId</param>
        /// <param name="e">email</param>
        /// <param name="cache"></param>
        /// <returns></returns>
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