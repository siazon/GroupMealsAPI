using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using App.Domain.Common;
using App.Domain.Common.Auth;
using App.Domain.Common.Customer;
using App.Domain.Config;
using App.Domain.TravelMeals;
using App.Domain.TravelMeals.Restaurant;
using App.Domain.TravelMeals.VO;
using App.Infrastructure.Exceptions;
using App.Infrastructure.ServiceHandler.Common;
using App.Infrastructure.ServiceHandler.TravelMeals;
using App.Infrastructure.Utility.Common;
using KingfoodIO.Application.Filter;
using KingfoodIO.Common;
using KingfoodIO.Controllers.Common;
using KingfoodIO.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.VisualBasic;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Twilio.TwiML.Voice;

namespace KingfoodIO.Controllers.TravelMeals
{
    /// <summary>
    /// Status: None=0,UnAccepted=1,Accepted=2,Canceled=3,OpenOrder = 4, Settled=5,
    /// AcceptStatus: 0:Defult, 1:Accepted, 2:Declined, 3:HoldOn, 4:CanceledBeforeAccepted, 5:CanceledAfterAccepted
    /// </summary>
    [Route("api/[controller]/[action]")]
    public class TrRestaurantBookingController : BaseController
    {
        private readonly ITrRestaurantBookingServiceHandler _restaurantBookingServiceHandler;

        IMemoryCache _memoryCache;
        ILogManager _logger;
        IAmountCalculaterUtil _calculaterUtil;
        private readonly IShopServiceHandler _shopServiceHandler;
        private readonly ICustomerServiceHandler _customerServiceHandler;

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
           ICustomerServiceHandler customerServiceHandler, ITrRestaurantServiceHandler restaurantServiceHandler, IAmountCalculaterUtil calculaterUtil, IShopServiceHandler shopServiceHandler, ILogManager logger) :
            base(cachesettingConfig, memoryCache, redisCache, logger)
        {
            _logger = logger;
            _memoryCache = memoryCache;
            _restaurantBookingServiceHandler = restaurantBookingServiceHandler;
            _calculaterUtil = calculaterUtil;
            _shopServiceHandler = shopServiceHandler;
            _customerServiceHandler= customerServiceHandler;
        }



        [Idempotent]
        [HttpPost]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.OK)]
        [ServiceFilter(typeof(AuthActionFilter))]
        public async Task<IActionResult> MakeABooking([FromBody] PayCurrencyVO payCurrencyVO, int shopId)
        {
            string rawRequestBody = await Request.GetRawBodyAsync();
            _logger.LogDebug("MakeABooking: " + rawRequestBody);
            var authHeader = Request.Headers["Wauthtoken"];
            var user = new TokenEncryptorHelper().Decrypt<DbToken>(authHeader);
            return await ExecuteAsync(shopId, false,
                async () => await _restaurantBookingServiceHandler.MakeABooking(payCurrencyVO, shopId, user));
        }

        [HttpGet]
        [ServiceFilter(typeof(AuthActionFilter))]
        [ProducesResponseType(typeof(List<TrDbRestaurant>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetBooking(string Id, int shopId, bool cache = false)
        {
            return await ExecuteAsync(shopId, cache, async () => await _restaurantBookingServiceHandler.GetBooking(Id));
        }
        [HttpGet]
        [ProducesResponseType(typeof(List<TrDbRestaurant>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> OrderCheck(bool cache = false)
        {
            return await ExecuteAsync(11, cache, async () => await _restaurantBookingServiceHandler.OrderCheck());
        }

        [HttpGet]
        [ServiceFilter(typeof(AuthActionFilter))]
        [ProducesResponseType(typeof(List<TrDbRestaurant>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> UpdateStatusByAdmin(string Id, int status, int shopId, bool cache = false)
        {
            var authHeader = Request.Headers["Wauthtoken"];
            var user = new TokenEncryptorHelper().Decrypt<DbToken>(authHeader);
            return await ExecuteAsync(shopId, cache, async () => await _restaurantBookingServiceHandler.UpdateStatusByAdmin(Id, status, user));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="booking"></param>
        /// <param name="shopId"></param>
        /// <param name="isNotify"></param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(typeof(DbBooking), (int)HttpStatusCode.OK)]
        [ServiceFilter(typeof(AuthActionFilter))]
        public async Task<IActionResult> ModifyBooking([FromBody] DbBooking booking, int shopId, bool isNotify = true)
        {
            string rawRequestBody = await Request.GetRawBodyAsync();
            _logger.LogDebug("ModifyBooking: " + rawRequestBody);
            var authHeader = Request.Headers["Wauthtoken"];
            var temp = new TokenEncryptorHelper().Decrypt<DbToken>(authHeader);
            return await ExecuteAsync(shopId, false,
                async () => await _restaurantBookingServiceHandler.ModifyBooking(booking, shopId, temp.UserEmail, isNotify));
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
            var user = new TokenEncryptorHelper().Decrypt<DbToken>(authHeader);
            bool IsAdmin = user.RoleLevel.AuthVerify(8);
            return await ExecuteAsync(shopId, cache,
                async () => await _restaurantBookingServiceHandler.CancelBooking(bookingId, detailId, user.UserEmail, IsAdmin));
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
        public async Task<IActionResult> SettleBooking(int shopId, string bookingId, string detailId, bool cache = false)
        {

            var authHeader = Request.Headers["Wauthtoken"];
            var temp = new TokenEncryptorHelper().Decrypt<DbToken>(authHeader);
            return await ExecuteAsync(shopId, cache,
                async () => await _restaurantBookingServiceHandler.SettleBooking(bookingId, detailId, temp.UserEmail));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="shopId"></param>
        /// <param name="bookingId"></param>
        /// <param name="detailId"></param>
        /// <param name="remark"></param>
        /// <param name="cache"></param>
        /// <returns></returns>
        [HttpGet]
        [ServiceFilter(typeof(AuthActionFilter))]
        [ProducesResponseType(typeof(List<TrDbRestaurant>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> UpsetBookingRemark(int shopId, string bookingId, string detailId, string remark, bool cache = false)
        {

            var authHeader = Request.Headers["Wauthtoken"];
            var temp = new TokenEncryptorHelper().Decrypt<DbToken>(authHeader);
            return await ExecuteAsync(shopId, cache,
                async () => await _restaurantBookingServiceHandler.UpsetBookingRemark(bookingId, detailId, remark, temp.UserEmail));
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
        /// <param name="shopId"></param>
        /// <param name="bookingId"></param>
        /// <param name="detailId"></param>
        /// <param name="cache"></param>
        /// <returns></returns>
        [HttpGet]
        [ServiceFilter(typeof(AuthActionFilter))]
        [ProducesResponseType(typeof(List<TrDbRestaurant>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> UndoDeleteDetail(int shopId, string bookingId, string detailId, bool cache = false)
        {
            return await ExecuteAsync(shopId, cache,
                async () => await _restaurantBookingServiceHandler.UndoDeleteDetail(bookingId, detailId, shopId));
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
        [ProducesResponseType(typeof(List<DbBooking>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> SearchBookings([FromBody] string pageToken, int shopId, string content, int pageSize = -1, bool cache = true)
        {
            var authHeader = Request.Headers["Wauthtoken"];
            var userInfo = new TokenEncryptorHelper().Decrypt<DbToken>(authHeader);

            return await ExecuteAsync(shopId, cache,
                async () => await _restaurantBookingServiceHandler.SearchBookings(shopId, userInfo.UserId, content, pageSize, pageToken));
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
        [ProducesResponseType(typeof(List<DbBooking>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> SearchBookingsByRestaurant([FromBody] string pageToken, int shopId, string content, int pageSize = -1, bool cache = true)
        {
            var authHeader = Request.Headers["Wauthtoken"];
            var userInfo = new TokenEncryptorHelper().Decrypt<DbToken>(authHeader);
            string email = userInfo.UserEmail;
            return await ExecuteAsync(shopId, cache,
                async () => await _restaurantBookingServiceHandler.SearchBookingsByRestaurant(shopId, email, content, pageSize, pageToken));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pageToken"></param>
        /// <param name="shopId"></param>
        /// <param name="content"></param>
        /// <param name="stime"></param>
        /// <param name="etime"></param>
        /// <param name="status"></param>
        /// <param name="isDelete"></param>
        /// <param name="pageSize"></param>
        /// <param name="cache"></param>
        /// <returns></returns>

        [HttpPost]
        [ServiceFilter(typeof(AuthActionFilter))]
        [ProducesResponseType(typeof(List<DbBooking>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> SearchBookingsByAdmin([FromBody] string pageToken, int shopId, string content, int filterTime, DateTime stime, DateTime etime, int status, int pageSize = -1, bool cache = true)
        {

            return await ExecuteAsync(shopId, cache,
                async () => await _restaurantBookingServiceHandler.SearchBookingsByAdmin(shopId, content, filterTime, stime, etime, status, pageSize, pageToken));
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="shopId"></param>
        /// <param name="bookingId"></param>
        /// <param name="cache"></param>
        /// <returns></returns>
        [HttpGet]
        [Idempotent]
        //[ServiceFilter(typeof(AuthActionFilter))]
        [ProducesResponseType(typeof(List<TrDbRestaurant>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> ResendEmail(int shopId, string bookingId, bool cache = false)
        {

            return await ExecuteAsync(shopId, cache,
                async () => await _restaurantBookingServiceHandler.ResendEmail(bookingId));
        }

        [HttpGet]
        [ServiceFilter(typeof(AuthActionFilter))]
        [ProducesResponseType(typeof(List<TrDbRestaurant>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> DoRebate(int shopId, string bookingId, double rebate, bool cache = false)
        {

            return await ExecuteAsync(shopId, cache,
                async () => await _restaurantBookingServiceHandler.DoRebate(bookingId, rebate));
        }
        [HttpGet]
        [ServiceFilter(typeof(AuthActionFilter))]
        [ProducesResponseType(typeof(List<TrDbRestaurant>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetDashboardData(int shopId, bool cache = false)
        {
            return await ExecuteAsync(shopId, cache,
            async () => await _restaurantBookingServiceHandler.GetDashboardData());
        }

        [HttpGet]
        [ServiceFilter(typeof(AuthActionFilter))]
        [ProducesResponseType(typeof(ResponseModel), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetSchedulePdf(int shopId, bool cache = false)
        {
            var authHeader = Request.Headers["Wauthtoken"];
            var user = new TokenEncryptorHelper().Decrypt<DbToken>(authHeader);
            return await ExecuteAsync(shopId, cache, async () => await _restaurantBookingServiceHandler.GetSchedulePdf(user));
        }

        /// <summary>
        /// 订单修改时返回的paidAmount不可用，应该取amountInfos里真实付了多少钱的Sum(paidAmount)。
        /// Json中menuCalculateType，price，childrenPrice，qty，childrenQty必填
        /// </summary>
        /// <param name="menuItems"></param>
        /// <param name="paymentType">来自Restaurant.billInfo 支付方式 0:全额,1:按比例,2:按固定值</param>
        /// <param name="payRate">来自Restaurant.billInfo 支付方式为1时必填，示例：15%押金传0.15</param>
        /// <param name="rewardType">来自用户信息</param>
        /// <param name="reward">来自用户信息</param>
        /// <param name="isOldCustomer">来自login中userInfo.isOldCustomer</param>
        /// <returns>paidAmount返回为0时，支付方式不显示</returns>
        [HttpPost]
        [ServiceFilter(typeof(AuthActionFilter))]
        [ProducesResponseType(typeof(ResponseModel), (int)HttpStatusCode.OK)]
        public  async Task<IActionResult> CalculateBookingItemAmount([FromBody] BookingCalculateVO bookingCalculateVO, PaymentTypeEnum rewardType, double reward, bool isOldCustomer)
        {
            var authHeader = Request.Headers["Wauthtoken"];
            var user = new TokenEncryptorHelper().Decrypt<DbToken>(authHeader);
            var customer = await _customerServiceHandler.GetCustomer(user.UserId, 11);
            return Ok(_restaurantBookingServiceHandler.GetBookingItemAmount(bookingCalculateVO, customer.RewardType, customer.Reward, isOldCustomer));
        }

        /// <summary>
        /// paymentMode: 1: paymentIntent,2: setupIntent
        /// </summary>
        /// <param name="cartInfoIds"></param>
        /// <param name="shopId"></param>
        /// <param name="isModify">是否订单修改</param>
        /// <param name="currency">isModify=false时必填</param>
        /// <param name="cache"></param>
        /// <returns></returns>
        [HttpPost]
        [ServiceFilter(typeof(AuthActionFilter))]
        [ProducesResponseType(typeof(ResponseModel), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> CalculateBookingAmount([FromBody] List<string> cartInfoIds, int shopId, bool isModify, string currency, bool cache = false)
        {
       
            DateTime sdate = DateTime.UtcNow;
            var authHeader = Request.Headers["Wauthtoken"];
            var user = new TokenEncryptorHelper().Decrypt<DbToken>(authHeader);

            Console.WriteLine("controller: " + (DateTime.UtcNow - sdate).TotalMilliseconds);
            return await ExecuteAsync(shopId, cache,
                async () => await _restaurantBookingServiceHandler.GetBookingAmount(isModify, currency, user.UserId, cartInfoIds));
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
        [Idempotent]
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
        [Idempotent]
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