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


        /// <summary>
        /// 
        /// </summary>
        /// <param name="booking"></param>
        /// <param name="shopId"></param>
        /// <returns></returns>
        [Idempotent]
        [HttpPost]
        [ProducesResponseType(typeof(TrDbRestaurantBooking), (int)HttpStatusCode.OK)]
        [ServiceFilter(typeof(AuthActionFilter))]
        public async Task<IActionResult> RequestTravelMealsBooking([FromBody] TrDbRestaurantBooking booking, int shopId)
        {
            string rawRequestBody = await Request.GetRawBodyAsync();
            _logger.LogDebug("RequestTravelMealsBooking: " + rawRequestBody);
            var authHeader = Request.Headers["Wauthtoken"];
            var user = new TokenEncryptorHelper().Decrypt<DbToken>(authHeader);
            return await ExecuteAsync(shopId, false,
                async () => await _restaurantBookingServiceHandler.RequestTravelMealsBooking(booking, shopId, user));
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
        //[ServiceFilter(typeof(RestaurantAuthFilter))]
        //[ServiceFilter(typeof(AuthActionFilter))]
        [ProducesResponseType(typeof(List<TrDbRestaurant>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> OrderCheck(bool cache = false)
        {
            return await ExecuteAsync(11, cache, async () => await _restaurantBookingServiceHandler.OrderCheck());
        }
        [HttpGet]
        [ProducesResponseType(typeof(List<TrDbRestaurant>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> ExportBooking(bool cache = false)
        {
            return await ExecuteAsync(11, cache, async () => await _restaurantBookingServiceHandler.ExportBooking());
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
        [HttpGet]
        [ServiceFilter(typeof(AuthActionFilter))]
        [ProducesResponseType(typeof(List<TrDbRestaurant>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> BookingAccepted(string Id, bool cache = false)
        {
            var authHeader = Request.Headers["Wauthtoken"];
            var user = new TokenEncryptorHelper().Decrypt<DbToken>(authHeader);
            return await ExecuteAsync(11, cache, async () => await _restaurantBookingServiceHandler.BookingAccepted(Id,user.UserEmail));
        }
        [HttpGet]
        [ProducesResponseType(typeof(List<TrDbRestaurant>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> BookingDeclined(string Id,string Reason, bool cache = false)
        {
            var authHeader = Request.Headers["Wauthtoken"];
            var user = new TokenEncryptorHelper().Decrypt<DbToken>(authHeader);
            return await ExecuteAsync(11, cache, async () => await _restaurantBookingServiceHandler.BookingDeclined(Id,Reason,user.UserEmail));
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
        /// <param name="booking"></param>
        /// <param name="shopId"></param>
        /// <param name="isNotify"></param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(typeof(DbBooking), (int)HttpStatusCode.OK)]
        [ServiceFilter(typeof(AuthActionFilter))]
        public async Task<IActionResult> ModifyBookingV1([FromBody] DbBooking booking, int shopId, bool isNotify = true)
        {
            string rawRequestBody = await Request.GetRawBodyAsync();
            _logger.LogDebug("ModifyBooking: " + rawRequestBody);
            var authHeader = Request.Headers["Wauthtoken"];
            var temp = new TokenEncryptorHelper().Decrypt<DbToken>(authHeader);
            return await ExecuteAsync(shopId, false,
                async () => await _restaurantBookingServiceHandler.ModifyBookingV1(booking, shopId, temp.UserEmail, isNotify));
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
            bool IsAdmin = user.RoleLevel.AuthVerify((ulong)AuthEnum.Admin);
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
        /// <param name="email">TA������</param>
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
                async () => await _restaurantBookingServiceHandler.SearchBookings(shopId, userInfo, content, pageSize, pageToken));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pageToken"></param>
        /// <param name="shopId"></param>
        /// <param name="email">TA������</param>
        /// <param name="content"></param>
        /// <param name="pageSize"></param>
        /// <param name="cache"></param>
        /// <returns></returns>
        [HttpPost]
        [ServiceFilter(typeof(AuthActionFilter))]
        [ProducesResponseType(typeof(List<DbBooking>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> SearchBookingsV1([FromBody] string pageToken, int shopId, string content, int pageSize = -1, bool cache = true)
        {
            var authHeader = Request.Headers["Wauthtoken"];
            var userInfo = new TokenEncryptorHelper().Decrypt<DbToken>(authHeader);

            return await ExecuteAsync(shopId, cache,
                async () => await _restaurantBookingServiceHandler.SearchBookingsV1(shopId, userInfo.UserId, content, pageSize, pageToken));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="queryParams">filterTime 0:CreateTime,1:selectedTime</param>
        /// <param name="shopId"></param>
        /// <param name="cache"></param>
        /// <returns></returns>
        [HttpPost]
        [ServiceFilter(typeof(RestaurantAuthFilter))]
        [ServiceFilter(typeof(AuthActionFilter))]
        [ProducesResponseType(typeof(List<DbBooking>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> SearchBookingsByRestaurant([FromBody] BookingQueryRestaurantVO queryParams, int shopId, bool cache = false)
        {
            var authHeader = Request.Headers["Wauthtoken"];
            var userInfo = new TokenEncryptorHelper().Decrypt<DbToken>(authHeader);
            string email = userInfo.UserEmail;
            return await ExecuteAsync(shopId, cache,
                async () => await _restaurantBookingServiceHandler.SearchBookingsByRestaurant(shopId, email, queryParams.content, queryParams.filterTime, queryParams.stime, queryParams.etime, queryParams.status, queryParams.pageSize, queryParams.continuationToken));
        }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="pageToken"></param>
    /// <param name="shopId"></param>
    /// <param name="content"></param>
    /// <param name="filterTime">0:CreateTime,1:selectedTime</param>
    /// <param name="stime"></param>
    /// <param name="etime"></param>
    /// <param name="status"></param>
    /// <param name="pageSize"></param>
    /// <param name="cache"></param>
    /// <returns></returns>

        [HttpPost]
        [ServiceFilter(typeof(AuthActionFilter))]
        [ProducesResponseType(typeof(List<DbBooking>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> SearchBookingsByAdmin([FromBody] BookingQueryRestaurantVO queryParams, int shopId,  bool cache = false)
        {

            return await ExecuteAsync(shopId, cache,
                async () => await _restaurantBookingServiceHandler.SearchBookingsByAdmin(shopId, queryParams.content, queryParams.filterTime, queryParams.stime, queryParams.etime, queryParams.status, queryParams.pageSize, queryParams.continuationToken));
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
        /// �����޸�ʱ���ص�paidAmount�����ã�Ӧ��ȡamountInfos����ʵ���˶���Ǯ��Sum(paidAmount)��
        /// Json��menuCalculateType��price��childrenPrice��qty��childrenQty����
        /// </summary>
        /// <param name="menuItems"></param>
        /// <param name="paymentType">����Restaurant.billInfo ֧����ʽ 0:ȫ��,1:������,2:���̶�ֵ</param>
        /// <param name="payRate">����Restaurant.billInfo ֧����ʽΪ1ʱ���ʾ����15%Ѻ��0.15</param>
        /// <param name="rewardType">�����û���Ϣ</param>
        /// <param name="reward">�����û���Ϣ</param>
        /// <param name="isOldCustomer">����login��userInfo.isOldCustomer</param>
        /// <returns>paidAmount����Ϊ0ʱ��֧����ʽ����ʾ</returns>
        [HttpPost]
        [ServiceFilter(typeof(AuthActionFilter))]
        [ProducesResponseType(typeof(ResponseModel), (int)HttpStatusCode.OK)]
        public  async Task<IActionResult> CalculateBookingItemAmountV1([FromBody] BookingCalculateVO bookingCalculateVO, PaymentTypeEnum rewardType, double reward, bool isOldCustomer,double vat)
        {
            //var authHeader = Request.Headers["Wauthtoken"];
            //var user = new TokenEncryptorHelper().Decrypt<DbToken>(authHeader);
            return Ok(_restaurantBookingServiceHandler.GetBookingItemAmountV1(bookingCalculateVO, bookingCalculateVO.BillInfo.RewardType, bookingCalculateVO.BillInfo.Reward, isOldCustomer,vat));
        }
        [HttpPost]
        [ServiceFilter(typeof(AuthActionFilter))]
        [ProducesResponseType(typeof(ResponseModel), (int)HttpStatusCode.OK)]
        public IActionResult CalculateBookingItemAmount([FromBody] List<BookingCourse> menuItems, PaymentTypeEnum paymentType, double payRate)
        {
            return Ok(_restaurantBookingServiceHandler.GetBookingItemAmount(menuItems, paymentType, payRate));
        }

        /// <summary>
        /// paymentMode: 1: paymentIntent,2: setupIntent
        /// </summary>
        /// <param name="cartInfoIds"></param>
        /// <param name="shopId"></param>
        /// <param name="isModify">�Ƿ񶩵��޸�</param>
        /// <param name="currency">isModify=falseʱ����</param>
        /// <param name="cache"></param>
        /// <returns></returns>
        [HttpPost]
        [ServiceFilter(typeof(AuthActionFilter))]
        [ProducesResponseType(typeof(ResponseModel), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> CalculateBookingAmountV1([FromBody] List<string> cartInfoIds, int shopId, bool isModify, string currency, bool cache = false)
        {
       
            DateTime sdate = DateTime.UtcNow;
            var authHeader = Request.Headers["Wauthtoken"];
            var user = new TokenEncryptorHelper().Decrypt<DbToken>(authHeader);

            Console.WriteLine("controller: " + (DateTime.UtcNow - sdate).TotalMilliseconds);
            return await ExecuteAsync(shopId, cache,
                async () => await _restaurantBookingServiceHandler.GetBookingAmountV1(isModify, currency, user.UserId, cartInfoIds));
        }

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
        /// <param name="acceptType">1�����գ�2�ܾ�</param>
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