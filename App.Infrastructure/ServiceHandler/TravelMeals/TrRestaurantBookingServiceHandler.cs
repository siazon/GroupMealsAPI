using App.Domain.TravelMeals.Restaurant;
using App.Domain.TravelMeals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using App.Domain.Common.Shop;
using App.Infrastructure.Builders.Common;
using App.Infrastructure.Repository;
using App.Infrastructure.Utility.Common;
using App.Domain.Common.Stripe;
using Stripe;
using App.Domain.Holiday;
using App.Infrastructure.Validation;
using App.Infrastructure.ServiceHandler.Tour;
using Hangfire;
using Microsoft.AspNetCore.Hosting;
using App.Infrastructure.Exceptions;
using Quartz.Impl;
using Quartz;
using Microsoft.Extensions.Caching.Memory;
using App.Domain.Enum;
using Microsoft.AspNetCore.Html;
using Stripe.FinancialConnections;
using Microsoft.CodeAnalysis.Text;
using App.Domain.Common.Customer;
using App.Domain.Common;
using App.Infrastructure.ServiceHandler.Common;
using Newtonsoft.Json;
using Twilio.Jwt.AccessToken;
using Microsoft.Azure.Cosmos;
using App.Domain.Common.Auth;
using Hangfire.Dashboard;
using Microsoft.Azure.Cosmos.Linq;
using App.Infrastructure.Extensions;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using App.Domain.Common.Email;
using System.Threading;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using QuestPDF.Helpers;
using System.Reflection.Metadata;
using System.Net.Http;
using RazorLight.Extensions;
using StackExchange.Redis;
using Twilio.Base;
using static FluentValidation.Validators.PredicateValidator;
using App.Domain.Config;
using QuestPDF.Fluent;
using SixLabors.ImageSharp.Memory;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using App.Domain.TravelMeals.VO;
using static Pipelines.Sockets.Unofficial.SocketConnection;
using System.Numerics;
using System.IO.Pipes;
using static System.Net.Mime.MediaTypeNames;
using System.Collections;
using System.Security.Policy;
using System.ComponentModel;
using System.Reflection;
using Microsoft.AspNetCore.Builder.Extensions;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using FirebaseAdmin.Messaging;
using Microsoft.AspNetCore.Http;
using Twilio.TwiML.Messaging;
using FirebaseAdmin.Auth;
using System.Linq.Expressions;
using FastDeepCloner;

namespace App.Infrastructure.ServiceHandler.TravelMeals
{
    public interface ITrRestaurantBookingServiceHandler
    {
        Task<DbBooking> GetBooking(string id);
        Task<TrDbRestaurantBooking> GetBookingOld(string id);
        Task<TrDbRestaurantBooking> UpdateBookingOld(TrDbRestaurantBooking booking);
        Task<bool> UpdateBooking(string billId, string productId, string priceId);
        Task<bool> BookingPaid(string bookingId, string customerId = "", string chargeId = "", string payMethodId = "", string receiptUrl = "");
        Task<bool> SavePayKeyCustomerId(string userId, string customerId, string intentId, string secertKey);
        Task<bool> ResendEmail(string bookingId);
        Task<bool> OrderCheck();
        Task<ResponseModel> ExportBooking();
        Task<bool> DoRebate(string bookingId, double rebate);
        Task<ResponseModel> GetDashboardData();
        Task<ResponseModel> GetSchedulePdf(DbToken userId);
        Task<ResponseModel> UpdateAccepted(string bookingId, string subBillId, int acceptType, string operater);
        Task<bool> UpdateAcceptedReason(string bookingId, string subBillId, string reason, string operater);
        Task<ResponseModel> BookingAccepted(string billId, string operater);
        Task<ResponseModel> BookingDeclined(string billId, string declineReason, string operater);
        Task<ResponseModel> CancelBooking(string bookingId, string detailId, string userEmail, bool isAdmin);
        Task<ResponseModel> SettleBooking(string bookingId, string detailId, string userEmail);
        Task<ResponseModel> UpdateStatusByAdmin(string id, int status, DbToken user);

        Task<ResponseModel> UpsetBookingRemark(string bookingId, string detailId, string remark, string userEmail);
        Task<ResponseModel> MakeABooking(PayCurrencyVO booking, int shopId, DbToken user);
        Task<ResponseModel> RequestTravelMealsBooking(TrDbRestaurantBooking booking, int shopId, DbToken user);
        Task<ResponseModel> ModifyBooking(DbBooking booking, int shopId, string email, bool isNotify = true);
        Task<ResponseModel> ModifyBookingV1(DbBooking booking, int shopId, string email, bool isNotify = true);
        Task<bool> DeleteBooking(string bookingId, int shopId);
        Task<bool> UndoDeleteDetail(string bookingId, string detailId, int shopId);
        Task<ResponseModel> SearchBookingsV1(int shopId, string userId, string content, int pageSize = -1, string continuationToke = null);
        Task<ResponseModel> SearchBookings(int shopId, DbToken user, string content, int pageSize = -1, string continuationToken = null);
        Task<ResponseModel> SearchBookingsByRestaurant(int shopId, string email, string content, int filterTime, DateTime stime, DateTime etime, List<int> status, int pageSize = -1, string continuationToken = null);
        Task<ResponseModel> SearchBookingsByAdmin(int shopId, string content, int filterTime, DateTime stime, DateTime etime, List<int> status, int pageSize = -1, string continuationToken = null);
        Task<List<DbBooking>> PlaceBooking(List<DbBooking> cartInfos, int shopId, DbCustomer user, IntentTypeEnum intentType);
        ResponseModel GetBookingItemAmount(List<BookingCourse> menuItems, PaymentTypeEnum paymentType, double payRate);
        ResponseModel GetBookingItemAmountV1(BookingCalculateVO bookingCalculateVO, PaymentTypeEnum rewardType, double reward, bool isOldCustomer, double vat);
        Task<ResponseModel> GetBookingAmountV1(bool isBookingModify, string currency, string userId, List<string> Ids);
        Task<ResponseModel> GetBookingAmount(bool isBookingModify, string currency, string userId, List<string> Ids);
        void SetupPaymentAction(string billId, string userId);
        void BookingCharged(string billId, string bookingIds, string ChargeId, string ReceiptUrl);
        void BookingChargedOld(string billId, string ChargeId, string ReceiptUrl);
        Task<List<DbBooking>> GetAllBookings(bool refreshCache = false);
        void SettleOrder();
    }
    public class TrRestaurantBookingServiceHandler : ITrRestaurantBookingServiceHandler
    {
        private readonly IDbCommonRepository<TrDbRestaurant> _restaurantRepository;
        private readonly IDbCommonRepository<TrDbRestaurantBooking> _restaurantBookingRepository;
        private readonly IDbCommonRepository<StripeCheckoutSeesion> _stripeCheckoutSeesionRepository;
        private readonly IDbCommonRepository<DbCustomer> _customerRepository;
        private readonly IDbCommonRepository<DbBooking> _bookingRepository;
        private readonly IDbCommonRepository<DbPaymentInfo> _paymentRepository;
        private readonly IDbCommonRepository<DbOpearationInfo> _opearationRepository;
        IFCMUtil _FCMUtil;
        IStripeServiceHandler _stripeServiceHandler;
        private readonly GMWebSocketManager _webSocketManager;
        private readonly ICountryServiceHandler _countryHandler;
        private readonly IShopServiceHandler _shopServiceHandler;
        private readonly IContentBuilder _contentBuilder;
        ITrRestaurantServiceHandler _trRestaurantServiceHandler;
        ISendEmailUtil _sendEmailUtil;
        ILogManager _logger;
        ITwilioUtil _twilioUtil;
        IStripeUtil _stripeUtil;
        private readonly IDbCommonRepository<DbShop> _shopRepository;
        private readonly ICustomerServiceHandler _customerServiceHandler;
        private readonly IExchangeUtil _exchangeUtil;
        IMemoryCache _memoryCache;
        private readonly IDateTimeUtil _dateTimeUtil;
        IAmountCalculaterUtil _amountCalculaterV1;
        IPDFUtil _pDFUtil;
        private readonly AzureStorageConfig storageConfig;
        IMsgPusherServiceHandler _msgPusherServiceHandler;

        public TrRestaurantBookingServiceHandler(ITwilioUtil twilioUtil, IDbCommonRepository<TrDbRestaurant> restaurantRepository, IFCMUtil FCMUtil,
            IDbCommonRepository<TrDbRestaurantBooking> restaurantBookingRepository, ITrRestaurantServiceHandler trRestaurantServiceHandler,
        IDbCommonRepository<DbCustomer> customerRepository, GMWebSocketManager webSocketManager, IMsgPusherServiceHandler msgPusherServiceHandler,
            IDbCommonRepository<DbShop> shopRepository, ICustomerServiceHandler customerServiceHandler, IStripeUtil stripeUtil, IMemoryCache memoryCache,
            IShopServiceHandler shopServiceHandler, IStripeServiceHandler stripeServiceHandler, ICountryServiceHandler countryHandler,
            IDbCommonRepository<StripeCheckoutSeesion> stripeCheckoutSeesionRepository, IDateTimeUtil dateTimeUtil, IAmountCalculaterUtil amountCalculaterV1,
            ISendEmailUtil sendEmailUtil, IExchangeUtil exchangeUtil, Microsoft.Extensions.Options.IOptions<AzureStorageConfig> _storageConfig,
            IDbCommonRepository<DbBooking> bookingRepository, IDbCommonRepository<DbPaymentInfo> paymentRepository, IDbCommonRepository<DbOpearationInfo> opearationRepository,
            IPDFUtil pDFUtil, ILogManager logger, IContentBuilder contentBuilder)
        {
            _restaurantRepository = restaurantRepository;
            _restaurantBookingRepository = restaurantBookingRepository;
            _trRestaurantServiceHandler = trRestaurantServiceHandler;
            _stripeCheckoutSeesionRepository = stripeCheckoutSeesionRepository;
            _bookingRepository = bookingRepository;
            _paymentRepository = paymentRepository;
            _opearationRepository = opearationRepository;
            _customerRepository = customerRepository;
            _stripeServiceHandler = stripeServiceHandler;
            _contentBuilder = contentBuilder;
            _countryHandler = countryHandler;
            _sendEmailUtil = sendEmailUtil;
            _exchangeUtil = exchangeUtil;
            storageConfig = _storageConfig.Value;
            _webSocketManager = webSocketManager;
            _FCMUtil = FCMUtil;
            _logger = logger;
            _pDFUtil = pDFUtil;
            _twilioUtil = twilioUtil;
            _shopRepository = shopRepository;
            _customerServiceHandler = customerServiceHandler;
            _stripeUtil = stripeUtil;
            _memoryCache = memoryCache;
            _dateTimeUtil = dateTimeUtil;
            _amountCalculaterV1 = amountCalculaterV1;
            _shopServiceHandler = shopServiceHandler;
            _msgPusherServiceHandler = msgPusherServiceHandler;
        }

        public async Task<TrDbRestaurantBooking> UpdateBookingOld(TrDbRestaurantBooking booking)
        {
            var res = await _restaurantBookingRepository.UpsertAsync(booking);
            return res;
        }

        public async Task<TrDbRestaurantBooking> GetBookingOld(string id)
        {
            var booking = await _restaurantBookingRepository.GetOneAsync(a => a.Id == id);
            return booking;
        }
        public async Task<DbBooking> GetBooking(string id)
        {
            var Booking = await _bookingRepository.GetOneAsync(r => r.Id == id);
            return Booking;
        }

        public async Task<ResponseModel> SettleBooking(string bookingId, string detailId, string userEmail)
        {
            var booking = await _bookingRepository.GetOneAsync(a => a.Id == detailId);
            if (booking == null || booking.AcceptStatus == AcceptStatusEnum.Declined || booking.AcceptStatus == AcceptStatusEnum.CanceledBeforeAccepted ||
                booking.AcceptStatus == AcceptStatusEnum.CanceledAfterAccepted || booking.Status == OrderStatusEnum.Canceled || booking.Status == OrderStatusEnum.None)
            {
                return new ResponseModel() { code = 501, msg = "订单状态不对，无法结单" };
            }
            var oldBooking = booking.Clone();
            booking.AcceptStatus = AcceptStatusEnum.SettledByAdmin;
            booking.Status = OrderStatusEnum.Settled;
            await _bookingRepository.UpsertAsync(booking);
            DbOpearationInfo operationInfo = new DbOpearationInfo() { ModifyType = 3, ReferenceId = booking.Id, Operater = userEmail, UpdateTime = DateTime.UtcNow, Operation = "结单" };
            UpdateField(operationInfo, oldBooking, booking, "Status");
            await _opearationRepository.UpsertAsync(operationInfo);
            var paymentInfo = await _paymentRepository.GetOneAsync(a => a.Id == booking.PaymentId);
            PayAction(paymentInfo, booking, true);
            SettleBookingOld(bookingId, detailId, userEmail);
            return new ResponseModel() { code = 200, msg = "ok" };
        }
        public async void SettleBookingOld(string bookingId, string detailId, string userEmail)
        {
            var booking = await _restaurantBookingRepository.GetOneAsync(a => a.Id == bookingId);
            foreach (var item in booking.Details)
            {
                if (item.Id == detailId)
                {
                    item.Status = OrderStatusEnum.Settled;
                    item.AcceptStatus = AcceptStatusEnum.SettledByAdmin;
                }
            }
            booking.Status = OrderStatusEnum.Settled;
            await _restaurantBookingRepository.UpsertAsync(booking);
        }
        public async Task<ResponseModel> UpsetBookingRemark(string bookingId, string detailId, string remark, string userEmail)
        {
            var booking = await _bookingRepository.GetOneAsync(a => a.Id == detailId);
            booking.Remark = remark;
            booking.Updater = userEmail;
            booking.Updated = DateTime.UtcNow;
            await _bookingRepository.UpsertAsync(booking);
            return new ResponseModel() { code = 200, msg = "ok" };
        }
        public async Task<ResponseModel> CancelBooking(string bookingId, string detailId, string userEmail, bool isAdmin)
        {


            var booking = await _bookingRepository.GetOneAsync(a => a.Id == bookingId);
            if (booking == null || booking.Status == OrderStatusEnum.Canceled)
                return new ResponseModel() { code = 501, msg = "订单已取消" };
            else
            {

                //if (!isAdmin && (item.SelectDateTime.Value.GetLocaTimeByIANACode(_dateTimeUtil.GetIANACode(item.RestaurantCountry)) - DateTime.UtcNow.GetLocaTimeByIANACode(_dateTimeUtil.GetIANACode(item.RestaurantCountry))).TotalHours < 24)
                //{
                //return new { code = 200, msg = "距离用餐时间24小时内取消请联系客服人员：微信：groupmeals", };
                //}
            }
            booking.Status = OrderStatusEnum.Canceled;
            if (booking.AcceptStatus == AcceptStatusEnum.Declined) { }//已拒绝不作反应
            else if (booking.AcceptStatus == AcceptStatusEnum.Accepted)
                booking.AcceptStatus = AcceptStatusEnum.CanceledAfterAccepted;
            else
                booking.AcceptStatus = AcceptStatusEnum.CanceledBeforeAccepted;

            CancelbookingNotification(booking);

            booking.Status = OrderStatusEnum.Canceled;
            booking.Updated = DateTime.UtcNow;
            booking.Updater = userEmail;
            DbOpearationInfo operationInfo = new DbOpearationInfo()
            {
                Id = Guid.NewGuid().ToString(),
                ModifyType = 3,
                ReferenceId = booking.Id,
                Operater = userEmail,
                UpdateTime = DateTime.UtcNow,
                Operation = "订单取消"
            };
            await _opearationRepository.UpsertAsync(operationInfo);

            var savedRestaurant = await _bookingRepository.UpsertAsync(booking);
            //CancelOld(bookingId, detailId, userEmail, isAdmin);
            return new ResponseModel() { code = 200, msg = "ok" };

        }
        public async Task<ResponseModel> CancelOld(string bookingId, string detailId, string userEmail, bool isAdmin)
        {
            var booking = await _restaurantBookingRepository.GetOneAsync(a => a.Id == bookingId);

            foreach (var item in booking.Details)
            {
                if (item.Id == detailId)
                {
                    if (item.Status == OrderStatusEnum.Canceled)
                        return new ResponseModel { code = 501, msg = "订单已取消", };
                    else
                    {

                        //if (!isAdmin && (item.SelectDateTime.Value.GetLocaTimeByIANACode(_dateTimeUtil.GetIANACode(item.RestaurantCountry)) - DateTime.UtcNow.GetLocaTimeByIANACode(_dateTimeUtil.GetIANACode(item.RestaurantCountry))).TotalHours < 24)
                        //{
                        //return new { code = 200, msg = "距离用餐时间24小时内取消请联系客服人员：微信：groupmeals", };
                        //}
                    }
                    item.Status = OrderStatusEnum.Canceled;
                    if (item.AcceptStatus == AcceptStatusEnum.Declined) { }//已拒绝不作反应
                    else if (item.AcceptStatus == AcceptStatusEnum.Accepted)
                        item.AcceptStatus = AcceptStatusEnum.CanceledAfterAccepted;
                    else
                        item.AcceptStatus = AcceptStatusEnum.CanceledBeforeAccepted;
                    var shopInfo = await _shopRepository.GetOneAsync(r => r.ShopId == booking.ShopId && r.IsActive.HasValue && r.IsActive.Value);
                    if (shopInfo == null)
                    {
                        _logger.LogInfo("----------------Cannot find shop info" + booking.Id);
                        throw new ServiceException("Cannot find shop info");
                    }
                }
            }
            booking.Status = OrderStatusEnum.Canceled;
            booking.Updated = DateTime.UtcNow;
            booking.Updater = userEmail;
            OperationInfo operationInfo = new OperationInfo() { ModifyType = 3, Operater = userEmail, UpdateTime = DateTime.UtcNow, Operation = "订单取消" };
            booking.Operations.Add(operationInfo);

            var savedRestaurant = await _restaurantBookingRepository.UpsertAsync(booking);

            return new ResponseModel { code = 200, msg = "ok", };
        }
        public async Task<bool> UpdateBooking(string billId, string productId, string priceId)
        {
            DbBooking booking = await GetBooking(billId);
            if (booking == null) return false;
            //booking.PaymentInfos[0].StripeProductId = productId;
            //booking.PaymentInfos[0].StripePriceId = priceId;
            var temp = await _bookingRepository.UpsertAsync(booking);
            return true;
        }
        private void UpdateStatus(DbBooking item, int acceptType)
        {

            AcceptStatusEnum statusEnum = (AcceptStatusEnum)acceptType;
            if (statusEnum == AcceptStatusEnum.CanceledBeforeAccepted && item.AcceptStatus == AcceptStatusEnum.Accepted)
                item.AcceptStatus = AcceptStatusEnum.CanceledAfterAccepted;
            else
                item.AcceptStatus = statusEnum;
            switch (statusEnum)
            {
                case AcceptStatusEnum.UnAccepted:
                    item.Status = OrderStatusEnum.UnAccepted;
                    break;
                case AcceptStatusEnum.Accepted:
                    item.Status = OrderStatusEnum.Accepted;
                    break;
                case AcceptStatusEnum.Declined:
                case AcceptStatusEnum.CanceledBeforeAccepted:
                case AcceptStatusEnum.CanceledAfterAccepted:
                    item.Status = OrderStatusEnum.Canceled;
                    break;
                case AcceptStatusEnum.Settled:
                case AcceptStatusEnum.SettledByAdmin:
                    item.Status = OrderStatusEnum.Settled;
                    break;
                default:
                    break;
            }
        }
        public async Task<ResponseModel> UpdateStatusByAdmin(string id, int status, DbToken user)
        {
            DbBooking booking = await GetBooking(id);
            AcceptStatusEnum statusEnum = (AcceptStatusEnum)status;

            var oldBooking = booking.Clone();
            UpdateStatus(booking, status);
            var temp = await _bookingRepository.UpsertAsync(booking);
            if (temp != null)
            {
                var opt = new DbOpearationInfo() { Id = Guid.NewGuid().ToString(), ReferenceId = id, Operater = user.UserEmail, Operation = "状态修改", UpdateTime = DateTime.UtcNow };
                UpdateField(opt, oldBooking, temp, "Status");

                await _opearationRepository.UpsertAsync(opt);
            }
            //UpdateStatusByAdminOld(id, status, user);
            return new ResponseModel { msg = "ok", code = 200, data = new { } };
        }

        //public async void UpdateStatusByAdminOld(string id, int status, DbToken user)
        //{

        //    TrDbRestaurantBooking booking = await _restaurantBookingRepository.GetOneAsync(a => a.Details.Any(d => d.Id == id));
        //    if (booking == null) return;
        //    AcceptStatusEnum statusEnum = (AcceptStatusEnum)status;
        //    foreach (var item in booking.Details)
        //    {
        //        if (item.Id == id)
        //        {
        //            UpdateStatus(item, status);
        //        }
        //    }
        //    var opt = new OperationInfo() { Operater = user.UserId, Operation = statusEnum.ToString(), UpdateTime = DateTime.UtcNow };
        //    booking.Operations.Add(opt);
        //    var temp = await _restaurantBookingRepository.UpsertAsync(booking);

        //}
        public async Task<ResponseModel> BookingAccepted(string billId, string operater)
        {
            DbBooking booking = await _bookingRepository.GetOneAsync(a => a.Id == billId);
            if (booking == null || booking.IsDeleted)
                return new ResponseModel() { code = 501, msg = "Order Deleted(无效操作，订单已删除)", };
            switch (booking.AcceptStatus)
            {
                case AcceptStatusEnum.UnAccepted:
                    booking.Status = OrderStatusEnum.Accepted;
                    booking.AcceptStatus = AcceptStatusEnum.Accepted;
                    break;
                case AcceptStatusEnum.Accepted:
                    return new ResponseModel() { code = 501, msg = "Order Accepted(订单已接受，如需修改请联系客服)" };
                case AcceptStatusEnum.Declined:
                    return new ResponseModel() { code = 501, msg = "Order Declined(订单已被拒绝，如需修改请联系客服)" };
                case AcceptStatusEnum.CanceledBeforeAccepted:
                case AcceptStatusEnum.CanceledAfterAccepted:
                    return new ResponseModel() { code = 501, msg = "Order Declined(订单已取消，如需修改请联系客服)" };
                case AcceptStatusEnum.Settled:
                case AcceptStatusEnum.SettledByAdmin:
                    return new ResponseModel() { code = 501, msg = "Order Settled(已结单，如需修改请联系客服)" };
                default:
                    break;
            }
            var res = await _bookingRepository.UpsertAsync(booking);
            //TODO sendMsg
            return new ResponseModel() { code = 200, msg = "ok", data = null };
        }
        public async Task<ResponseModel> BookingDeclined(string billId, string declineReason, string operater)
        {
            _logger.LogDebug("BookingDeclined: " + declineReason);
            DbBooking booking = await _bookingRepository.GetOneAsync(a => a.Id == billId);
            if (booking == null || booking.IsDeleted)
                return new ResponseModel() { code = 501, msg = "Order Deleted(无效操作，订单已删除)", };
            switch (booking.AcceptStatus)
            {
                case AcceptStatusEnum.UnAccepted:
                    booking.Status = OrderStatusEnum.Canceled;
                    booking.AcceptStatus = AcceptStatusEnum.Declined;
                    break;
                case AcceptStatusEnum.Accepted:
                    return new ResponseModel() { code = 501, msg = "Order Accepted(订单已接受，如需修改请联系客服)" };
                case AcceptStatusEnum.Declined:
                    return new ResponseModel() { code = 501, msg = "Order Declined(订单已被拒绝，如需修改请联系客服)" };
                case AcceptStatusEnum.CanceledBeforeAccepted:
                case AcceptStatusEnum.CanceledAfterAccepted:
                    return new ResponseModel() { code = 501, msg = "Order Declined(订单已取消，如需修改请联系客服)" };
                case AcceptStatusEnum.Settled:
                case AcceptStatusEnum.SettledByAdmin:
                    return new ResponseModel() { code = 501, msg = "Order Settled(已结单，如需修改请联系客服)" };
                default:
                    break;
            }
            booking.AcceptReason = declineReason;
            var res = await _bookingRepository.UpsertAsync(booking);
            //TODO sendMsg
            return new ResponseModel() { code = 200, msg = "ok", data = null };
        }

        public async Task<ResponseModel> UpdateAccepted(string billId, string subBillId, int acceptType, string operater)
        {
            DbBooking booking = await _bookingRepository.GetOneAsync(a => a.Id == subBillId);

            if (booking == null || booking.IsDeleted)
                return new ResponseModel() { code = 501, msg = "Order Deleted(无效操作，订单已删除)", };
            switch (booking.AcceptStatus)
            {
                case AcceptStatusEnum.UnAccepted:
                    UpdateStatus(booking, acceptType);
                    break;
                case AcceptStatusEnum.Accepted:
                    var customer = await _customerRepository.GetOneAsync(a => a.Email == operater);
                    if (customer != null)
                    {
                        bool IsAdmin = customer.AuthValue.AuthVerify((ulong)AuthEnum.Admin);
                        if (IsAdmin)
                        {
                            UpdateStatus(booking, acceptType);
                        }
                        else
                        {
                            return new ResponseModel() { code = 501, msg = "Order Accepted(订单已接受，如需修改请联系客服)" };
                        }
                    }
                    break;
                case AcceptStatusEnum.Declined:
                    customer = await _customerRepository.GetOneAsync(a => a.Email == operater);
                    if (customer != null)
                    {
                        bool IsAdmin = customer.AuthValue.AuthVerify((ulong)AuthEnum.Admin);
                        if (IsAdmin)
                        {
                            UpdateStatus(booking, acceptType);
                        }
                        else
                        {
                            return new ResponseModel() { code = 501, msg = "Order Declined(订单已被拒绝，如需修改请联系客服)" };
                        }
                    }

                    break;
                case AcceptStatusEnum.CanceledBeforeAccepted:
                case AcceptStatusEnum.CanceledAfterAccepted:
                    customer = await _customerRepository.GetOneAsync(a => a.Email == operater);
                    if (customer != null)
                    {
                        bool IsAdmin = customer.AuthValue.AuthVerify((ulong)AuthEnum.Admin);
                        if (IsAdmin)
                        {
                            UpdateStatus(booking, acceptType);
                        }
                        else
                        {
                            return new ResponseModel() { code = 501, msg = "Order Declined(订单已取消，如需修改请联系客服)" };
                        }
                    }
                    break;
                case AcceptStatusEnum.Settled:
                case AcceptStatusEnum.SettledByAdmin:
                    return new ResponseModel() { code = 501, msg = "Order Settled(已结单，如需修改请联系客服)" };
                default:
                    break;
            }

            //if (acceptType == 2)
            //{
            //    _stripeUtil.RefundGroupMeals(booking);
            //}
            DbOpearationInfo opt = new DbOpearationInfo() { Id = Guid.NewGuid().ToString(), ReferenceId = booking.Id, Operater = operater, Operation = acceptType == 1 ? "接收预订" : "拒绝预订", UpdateTime = DateTime.UtcNow };
            await _opearationRepository.UpsertAsync(opt);
            var temp = await _bookingRepository.UpsertAsync(booking);
            string msg = $"您于{booking.Created.Value.ToString("yyyy-MM-dd HH:mm")}  提交的订单已被接收，请按时就餐";
            if (acceptType == 2)
            {
                msg = $"您于{booking.Created.Value.ToString("yyyy-MM-dd HH:mm")}  提交的订单已被拒绝，请登录groupmeals.com查询别的餐厅";
                _logger.LogInfo($"你有订单[{booking.BookingRef}]被餐厅拒绝了，请登录groupmeal.com查看更多");
                _twilioUtil.sendSMS("+353874858555", $"你有订单[{booking.BookingRef}]被餐厅拒绝了，请登录groupmeal.com查看更多");
            }
            //_twilioUtil.sendSMS(booking.CustomerPhone, msg);
            var shopInfo = await _shopRepository.GetOneAsync(r => r.ShopId == booking.ShopId && r.IsActive.HasValue && r.IsActive.Value);
            if (shopInfo == null)
            {
                _logger.LogInfo("----------------Cannot find shop info" + booking.Id);
                return new ResponseModel() { code = 501, msg = "Cannot find shop info" };
            }
            string mealTime = booking.SelectDateTime.Value.GetLocaTimeByIANACode(booking.RestaurantTimeZone).ToString("yyyy-MM-dd HH:mm");
            //await _msgPusherServiceHandler.AddMsg(new Domain.Common.PushMsgModel()
            //{
            //    Id = Guid.NewGuid().ToString(),
            //    MsgType = MsgTypeEnum.AcceptOrder,
            //    SendTime = DateTime.UtcNow,
            //    Created = DateTime.UtcNow,
            //    Title = "Booking Accepted",
            //    Message = $"{booking.RestaurantName} {mealTime}",
            //    MessageReference = booking.BookingRef,
            //    Receiver = booking.RestaurantEmail,
            //    Sender = "GroupMeals",
            //    ShopId = booking.ShopId
            //});
            SendEamilByUpdateAccept(acceptType, booking, shopInfo);
            return new ResponseModel() { code = 200, msg = "ok", data = booking };
        }


        private void SendEamilByUpdateAccept(int acceptType, DbBooking booking, DbShop shopInfo)
        {
            System.Threading.Tasks.Task.Run(async () =>
            {
                try
                {
                    //System.Threading.Thread.Sleep(1000 * 60);
                    if (acceptType == 1)
                    {
                        //   var emailParams = EmailConfigs.Instance.Emails[EmailTypeEnum.MealAccepted];
                        //   var userInfo = await _customerRepository.GetOneAsync(r => r.Id == booking.Creater);
                        //   emailParams.isShortInfo = 0;
                        //await   _sendEmailUtil.EmailEach(new List<DbBooking>() { booking }, shopInfo, emailParams);

                        var emailParamsRest = EmailConfigs.Instance.Emails[EmailTypeEnum.MealAcceptedRestaurant];
                        emailParamsRest.isShortInfo = 1;
                        await _sendEmailUtil.EmailEach(new List<DbBooking>() { booking }, shopInfo, emailParamsRest);

                    }
                    else if (acceptType == 2)
                    {
                        // var emailParams = EmailConfigs.Instance.Emails[EmailTypeEnum.MealDeclined];
                        // var userInfo = await _customerRepository.GetOneAsync(r => r.Id == booking.Creater);
                        // emailParams.isShortInfo = 0;
                        //await _sendEmailUtil.EmailEach(new List<DbBooking>() { booking }, shopInfo, emailParams);
                    }
                }
                catch (Exception ex)
                {
                }
            });
        }
        public async Task<bool> UpdateAcceptedReason(string billId, string subBillId, string reason, string operater)
        {
            DbBooking booking = await GetBooking(billId);
            booking.AcceptReason = reason;
            if (booking == null) return false;
            var temp = await _bookingRepository.UpsertAsync(booking);
            if (temp != null)
            {
                var opt = new DbOpearationInfo() { Operater = operater, ReferenceId = billId, Operation = "添加原因", UpdateTime = DateTime.UtcNow };
                await _opearationRepository.UpsertAsync(opt);
            }

            return true;
        }

        public async Task<bool> BookingPaid(string bookingId, string customerId = "", string chargeId = "", string payMethodId = "", string receiptUrl = "")
        {
            try
            {
                _logger.LogInfo("----------------BookingPaid");
                DbBooking booking = await _bookingRepository.GetOneAsync(r => 1 == 1);
                if (booking == null)
                {
                    _logger.LogInfo("----------------bookingId: [" + bookingId + "] not found");
                    return false;
                }
                var PaymentInfos = await _paymentRepository.GetOneAsync(a => a.StripeCustomerId == customerId);
                if (!string.IsNullOrWhiteSpace(payMethodId))
                    PaymentInfos.StripePaymentMethodId = payMethodId;
                if (!string.IsNullOrWhiteSpace(customerId))
                {
                    PaymentInfos.StripeCustomerId = customerId;
                    PaymentInfos.StripeSetupIntent = true;
                }
                if (!string.IsNullOrWhiteSpace(receiptUrl))
                {
                    PaymentInfos.StripeChargeId = chargeId;
                    PaymentInfos.StripeReceiptUrl = receiptUrl;
                    PaymentInfos.Paid = true;

                    booking.Status = Domain.Enum.OrderStatusEnum.UnAccepted;
                }
                _logger.LogInfo("----------------BookingPaid" + booking.Id);
                var temp = await _paymentRepository.UpsertAsync(PaymentInfos);
                var customer = await _customerRepository.GetOneAsync(a => a.Email == booking.ContactEmail);
                ClearCart(customer.Id, temp.ShopId ?? 11);
                var shopInfo = await _shopRepository.GetOneAsync(r => r.ShopId == booking.ShopId && r.IsActive.HasValue && r.IsActive.Value);
                if (shopInfo == null)
                {
                    _logger.LogInfo("----------------Cannot find shop info" + booking.Id);
                    throw new ServiceException("Cannot find shop info");
                }

            }
            catch (Exception ex)
            {
                _logger.LogInfo("----------------BookingPaid.err" + ex.Message + "： " + ex.StackTrace);
            }
            return true;
        }
        private async void ClearCart(string userId, int shopId)
        {

            var customer = await _customerRepository.GetOneAsync(a => a.Id == userId);

            if ((customer != null))
            {
                customer.CartInfos.Clear();

            }
        }
        public async Task<ResponseModel> ModifyBooking(DbBooking newBooking, int shopId, string email, bool isNotify = true)
        {
            return new ResponseModel { msg = "暂不支付订单修改，请联系客服人员", code = 501, data = null };
        }
        /// <summary>
        /// 线下支付时：直接修改金额并发邮件通知
        /// 线上支付时：新增一条支付差价记录
        /// </summary>
        /// <param name="booking"></param>
        /// <param name="shopId"></param>
        /// <param name="email"></param>
        /// <returns></returns>
        public async Task<ResponseModel> ModifyBookingV1(DbBooking newBooking, int shopId, string email, bool isNotify = true)
        {
            Guard.NotNull(newBooking);
            Guard.AreEqual(newBooking.ShopId.Value, shopId);
            //if (newBooking.Charged)
            //    return new ResponseModel { msg = "订单已扣款，不可再修改", code = 501, data = null };
            var dbBooking = await _bookingRepository.GetOneAsync(r => !r.IsDeleted && r.Id == newBooking.Id);
            if (dbBooking == null) return new ResponseModel { msg = "booking not found", code = 501, data = null };
            newBooking.BillInfo = dbBooking.BillInfo;
            newBooking.RestaurantIncluedVAT = dbBooking.RestaurantIncluedVAT;

            DbOpearationInfo operationInfo = new DbOpearationInfo()
            {
                Id = Guid.NewGuid().ToString(),
                ReferenceId = newBooking.Id,
                ModifyType = 4,
                Operater = email,
                UpdateTime = DateTime.UtcNow,
                Operation = "订单修改"
            };
            int isChange = 0; int isDtlChanged = 0;

            if (newBooking != null)
            {
                //if (detail.SelectDateTime != item.SelectDateTime)
                //{
                //item.Modified = true;
                //    isChange = true;
                //    ModifyInfo modifyInfo = new ModifyInfo();
                //    modifyInfo.ModifyField = nameof(item.SelectDateTime);
                //    modifyInfo.ModifyLocation = $"{booking.Id}>{item.Id}";
                //    modifyInfo.oldValue = item.SelectDateTime.ToString();
                //    modifyInfo.newValue = detail.SelectDateTime.ToString();
                //    operationInfo.ModifyInfos.Add(modifyInfo);
                //    item.SelectDateTime = detail.SelectDateTime;
                //}

                var res = UpdateField(operationInfo, dbBooking, newBooking, "SelectDateTime");
                if (res)
                {
                    //if (dbBooking.IntentType == IntentTypeEnum.PaymentIntent)
                    //{
                    //    return new ResponseModel { msg = "订单已支付，如需修改请联系客服人员", code = 501, data = null };
                    //}
                    isChange++;
                }
                res = UpdateField(operationInfo, dbBooking, newBooking, "Memo");
                if (res) isChange++;
                res = UpdateField(operationInfo, dbBooking, newBooking, "GroupRef");
                if (res) isChange++;
                res = UpdateField(operationInfo, dbBooking, newBooking, "ContactName");
                if (res) isChange++;
                res = UpdateField(operationInfo, dbBooking, newBooking, "ContactPhone");
                if (res) isChange++;
                res = UpdateField(operationInfo, dbBooking, newBooking, "ContactEmail");
                if (res) isChange++;
                res = UpdateField(operationInfo, dbBooking, newBooking, "ContactWechat");
                if (res) isChange++;
                res = UpdateField(operationInfo, dbBooking, newBooking, "ContactInfos");
                if (res) isChange++;
                res = UpdateField(operationInfo, dbBooking, newBooking, "Remark");
                if (res) isChange++;
                res = UpdateField(operationInfo, dbBooking, newBooking, "RestaurantId");
                //if (res)
                if (dbBooking.BillInfo.PaymentType != newBooking.BillInfo.PaymentType)
                {
                    return new ResponseModel { msg = "订单已支付，新餐厅支付方式不一致", code = 501, data = null };
                }


                var rest = await _restaurantRepository.GetOneAsync(a => a.Id == newBooking.RestaurantId);

                var Oldamount = _amountCalculaterV1.getItemAmount(dbBooking.ConvertToAmount());
                var amount = _amountCalculaterV1.getItemAmount(newBooking.ConvertToAmount());
                var user = await _customerServiceHandler.GetCustomer(dbBooking.Creater, dbBooking.ShopId ?? 11);
                var oldPayAmount = _amountCalculaterV1.getItemPayAmount(dbBooking.ConvertToAmount(), user, rest.Vat);
                var payAmount = _amountCalculaterV1.getItemPayAmount(newBooking.ConvertToAmount(), user, rest.Vat);
                if (amount != Oldamount)
                {
                    if (dbBooking.IntentType == IntentTypeEnum.PaymentIntent)
                    {
                        return new ResponseModel { msg = "订单已支付，如需修改请联系客服人员", code = 501, data = null };
                    }
                    isChange++;
                    UpdateListField(operationInfo, dbBooking, newBooking, "Courses");
                    dbBooking.Courses = newBooking.Courses;

                    ItemPayInfo amountInfo = new ItemPayInfo() { Id = Guid.NewGuid().ToString() };
                    amountInfo.Amount = amount - Oldamount;//新增差价记录
                    if (!dbBooking.BillInfo.IsOldCustomer)
                    {
                        amountInfo.PaidAmount = payAmount.PaidAmount - oldPayAmount.PaidAmount;
                        amountInfo.Reward = payAmount.Reward - oldPayAmount.Reward;
                        amountInfo.Vat = payAmount.Vat - oldPayAmount.Vat;
                        amountInfo.PaidAmount = payAmount.PaidAmount - oldPayAmount.PaidAmount;
                        amountInfo.Unpaid = payAmount.Unpaid - oldPayAmount.Unpaid;
                        amountInfo.Commission = payAmount.Commission - oldPayAmount.Commission;
                    }
                    dbBooking.AmountInfos.Add(amountInfo);
                    if (dbBooking.IntentType != IntentTypeEnum.SetupIntent)
                    {
                        if (payAmount.PaidAmount > oldPayAmount.PaidAmount)
                        {
                            DbPaymentInfo dbPaymentInfo = new DbPaymentInfo()
                            {
                                Id = Guid.NewGuid().ToString(),
                                Creater = user.Id,
                                Created = DateTime.UtcNow,
                                Amount = payAmount.PaidAmount,
                                CheckoutTime = DateTime.UtcNow,

                            };
                        }
                        else if (payAmount.PaidAmount < oldPayAmount.PaidAmount)
                        {

                        }
                    }
                }
                if (dbBooking.Courses.Count != newBooking.Courses.Count)
                {
                    isChange++;
                    UpdateListField(operationInfo, dbBooking, newBooking, "Courses");
                    dbBooking.Courses = newBooking.Courses;
                }
                for (int i = 0; i < dbBooking.Courses.Count; i++)
                {
                    if (dbBooking.Courses[i].ToString() != newBooking.Courses[i].ToString())
                    {
                        isChange++;
                        UpdateListField(operationInfo, dbBooking, newBooking, "Courses");
                        dbBooking.Courses = newBooking.Courses;
                    }
                }
                dbBooking.RestaurantName = rest.StoreName;
                dbBooking.RestaurantEmail = rest.Email;
                dbBooking.RestaurantAddress = rest.Address;
                dbBooking.RestaurantPhone = rest.PhoneNumber;
                dbBooking.EmergencyPhone = rest.ContactPhone;
                dbBooking.RestaurantWechat = rest.Wechat;
                dbBooking.Currency = rest.Currency;
                dbBooking.RestaurantTimeZone = rest.TimeZone;
                dbBooking.BillInfo = rest.BillInfo;

            }
            if (isDtlChanged == isChange)
                dbBooking.Modified = false;
            else
                dbBooking.Modified = true;
            isDtlChanged = isChange;
            if (isChange > 0)
            {
                await _opearationRepository.UpsertAsync(operationInfo);
                var savedBooking = await _bookingRepository.UpsertAsync(dbBooking);
                if (isNotify)
                    ModifybookingNotification(savedBooking);
            }
            return new ResponseModel { msg = "ok", code = 200, data = null };
        }

        private bool UpdateField(OperationInfo operationInfo, DbBooking item, DbBooking detail, string fieldName, bool record = true)
        {
            bool isChange = false;
            try
            {
                var newValue = detail.GetType().GetProperty(fieldName).GetValue(detail, null);
                var oldValue = item.GetType().GetProperty(fieldName).GetValue(item, null);
                if (newValue?.ToString() != oldValue?.ToString())
                {
                    item.Modified = true;
                    isChange = true;
                    if (record)
                    {
                        ModifyInfo modifyInfo = new ModifyInfo();
                        modifyInfo.ModifyField = fieldName;
                        modifyInfo.ModifyLocation = $"{item.Id}";
                        if (oldValue.GetType() == typeof(DateTime))
                            modifyInfo.oldValue = ((DateTime?)oldValue)?.ToString("o");
                        else
                            modifyInfo.oldValue = oldValue?.ToString();
                        if (newValue.GetType() == typeof(DateTime))
                            modifyInfo.newValue = ((DateTime?)newValue)?.ToString("o");
                        else
                            modifyInfo.newValue = newValue?.ToString();
                        operationInfo.ModifyInfos.Add(modifyInfo);
                    }
                    item.GetType().GetProperty(fieldName).SetValue(item, newValue);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("UpdateField." + fieldName + "： " + ex.Message);
            }

            return isChange;
        }
        private bool UpdateListField(OperationInfo operationInfo, DbBooking item, DbBooking detail, string fieldName)
        {
            bool isChange = false;
            var newValue = string.Join(",", (detail.GetType().GetProperty(fieldName).GetValue(detail, null) as List<BookingCourse>));
            var oldValue = string.Join(",", item.GetType().GetProperty(fieldName).GetValue(item, null) as List<BookingCourse>);
            if (newValue?.ToString() != oldValue?.ToString())
            {
                item.Modified = true;
                isChange = true;
                ModifyInfo modifyInfo = new ModifyInfo();
                modifyInfo.ModifyField = fieldName;
                modifyInfo.ModifyLocation = $"{item.Id}";
                modifyInfo.oldValue = oldValue?.ToString();
                modifyInfo.newValue = newValue?.ToString();
                operationInfo.ModifyInfos.Add(modifyInfo);
            }
            return isChange;
        }
        public async Task<ResponseModel> RequestTravelMealsBooking(TrDbRestaurantBooking booking, int shopId, DbToken user)
        {
            //Thread.Sleep(5000);
            _logger.LogInfo("RequestBooking" + user.UserEmail);
            Guard.NotNull(booking);
            booking.ShopId = shopId;
            Guard.AreEqual(booking.ShopId.Value, shopId);
            //foreach (var item in booking.Details)
            //{
            //    _logger.LogInfo(" Make a booking.time: " + item.SelectDateTime);
            //    if ((item.SelectDateTime - DateTime.UtcNow).Value.TotalHours < 12)
            //    {
            //        return new ResponseModel { msg = "用餐时间少于12个小时", code = 501, data = null };
            //    }
            //}
            TourBooking newBooking;
            var exsitBooking = await _restaurantBookingRepository.GetOneAsync(r => r.Status == OrderStatusEnum.None && !r.IsDeleted && r.CustomerEmail == user.UserEmail);
            if (exsitBooking != null)
            {
                booking.Id = exsitBooking.Id;
                booking.BookingRef = exsitBooking.BookingRef;
            }
            else
            {
                booking.Id = Guid.NewGuid().ToString();
                booking.BookingRef = "GM" + SnowflakeId.getSnowId();
                booking.Created = DateTime.UtcNow;
                var opt = new OperationInfo() { Operater = user.UserEmail, Operation = "新增订单", UpdateTime = DateTime.UtcNow };
                booking.Operations.Add(opt);
            }
            foreach (var item in booking.Details)
            {
                if (string.IsNullOrWhiteSpace(item.Currency))
                {
                    var rest = await _restaurantRepository.GetOneAsync(a => a.Id == item.RestaurantId);
                    if (rest != null)
                    {
                        item.Currency = rest.Country;
                    }
                }
                DateTime dateTime = item.SelectDateTime.Value;
                if (!string.IsNullOrWhiteSpace(item.MealTime))
                {
                    DateTime.TryParse(item.MealTime, out dateTime);
                    item.SelectDateTime = dateTime.GetTimeZoneByIANACode(item.RestaurantTimeZone);

                    string[] temp = item.MealTime.Split(' ');
                    string[] timetemp = temp[1].Split(':');
                    int hour = 11;
                    int.TryParse(timetemp[0], out hour);

                    if (item.SelectDateTime.Value.Year - DateTime.UtcNow.Year > 5)
                        return new ResponseModel { msg = "用餐时间不正确", code = 501, data = null };

                    if (hour < 11 || hour > 23)
                        return new ResponseModel { msg = "用餐时间不正确", code = 501, data = null };
                }
            }



            var amountInfo = await GetAmountInfoByOldVersion(booking.Details, booking.PayCurrency, user.UserId);

            DbPaymentInfo dbPaymentInfo = await _paymentRepository.GetOneAsync(a => a.Id == booking.Details[0].PaymentId);
            if (dbPaymentInfo == null)
            {
                dbPaymentInfo = new DbPaymentInfo();
                dbPaymentInfo.Id = Guid.NewGuid().ToString();
                dbPaymentInfo.Creater = user.UserId;
                dbPaymentInfo.Created = DateTime.UtcNow;
            }
            dbPaymentInfo.Amount = amountInfo.TotalPayAmount;
            bool noPay = amountInfo.TotalPayAmount == 0;

            await InitBooking(booking, user.UserId, noPay);
            string currency = "EUR";
            switch (booking.PayCurrency)
            {
                case "eu":
                    currency = "EUR";
                    break;
                case "uk":
                    currency = "GBP";
                    break;
                default:
                    break;
            }

            dbPaymentInfo.Currency = currency;


            booking.PaymentInfos.Add(new PaymentInfo()
            {
                Amount = amountInfo.TotalPayAmount,
                PaidAmount = amountInfo.TotalPayAmount * 100
            });
            booking.Details.ForEach(a => a.PaymentId = dbPaymentInfo.Id);
            if (!noPay)
                await _paymentRepository.UpsertAsync(dbPaymentInfo);
            var newItem = await _restaurantBookingRepository.UpsertAsync(booking);
            if (noPay)
            {
                var userInfo = await _customerServiceHandler.GetCustomer(user.UserId, shopId);
                var res = PlaceBooking(booking.Details, shopId, userInfo, IntentTypeEnum.None);
                await NewBookingNofitication(booking.Details, userInfo);
            }
            return new ResponseModel { msg = "ok", code = 200, data = newItem };
        }
        public async Task<ResponseModel> MakeABooking(PayCurrencyVO payCurrencyVO, int shopId, DbToken user)
        {
            Guard.NotNull(payCurrencyVO.BookingIds);
            _logger.LogInfo("RequestBooking" + user.UserEmail);
            var userInfo = await _customerRepository.GetOneAsync(r => r.Id == user.UserId && r.CartInfos.Count() > 0);

            if (userInfo == null)
            {
                return new ResponseModel { msg = "购物车为空", code = 501, };
            }


            List<DbBooking> bookings = new List<DbBooking>();
            List<string> bookingIdList = new List<string>();
            string paymentId = "";
            foreach (var item in userInfo.CartInfos)
            {
                if (payCurrencyVO.BookingIds.Count > 0 && !payCurrencyVO.BookingIds.Contains(item.Id)) continue;
                await InitBookingDetail(item, user.UserId);
                item.BillInfo.IsOldCustomer = userInfo.IsOldCustomer;
                bookings.Add(item);
                bookingIdList.Add(item.Id);
                paymentId = item.PaymentId;
            }
            DbPaymentInfo dbPaymentInfo = null;
            if (!string.IsNullOrWhiteSpace(paymentId))
                dbPaymentInfo = await _paymentRepository.GetOneAsync(a => a.Id == paymentId && !a.Paid);
            if (dbPaymentInfo == null || (dbPaymentInfo.SetupPay && payCurrencyVO.IntentType == IntentTypeEnum.PaymentIntent) ||
                (!dbPaymentInfo.SetupPay && payCurrencyVO.IntentType == IntentTypeEnum.SetupIntent))
            {
                dbPaymentInfo = new DbPaymentInfo();
                dbPaymentInfo.Id = Guid.NewGuid().ToString();
                dbPaymentInfo.Creater = userInfo.Id;
                dbPaymentInfo.Created = DateTime.UtcNow;
            }

            var countries = await _countryHandler.GetCountries(userInfo.ShopId ?? 11);
            var dbStripes = await _countryHandler.GetStripes();
            dbPaymentInfo.Amount = _amountCalculaterV1.CalculateOrderPaidAmount(bookings, payCurrencyVO.PayCurrency, userInfo, countries, dbStripes);
            dbPaymentInfo.Currency = payCurrencyVO.PayCurrency.ToUpper().Trim();

            var payAmount = _amountCalculaterV1.GetOrderPaidInfo(bookings, payCurrencyVO.PayCurrency, shopId, userInfo, countries, dbStripes);
            if (payAmount == null || payAmount.TotalPayAmount < 0)
            {
                return new ResponseModel { msg = "支付金额不可小于0，请联系客服人员", code = 501, };
            }

            if (dbPaymentInfo.Amount == 0)
            {
                var dbUser = await _customerServiceHandler.UpdateCart(bookings, user.UserId, user.ShopId ?? 11);
                var booking = PlaceBooking(bookings, shopId, userInfo, IntentTypeEnum.None);
                await NewBookingNofitication(bookings, userInfo);
                return new ResponseModel { msg = "ok", code = 200, data = null };
            }
            string bookingIds = string.Join(',', bookingIdList);
            var stripeKeys = await _countryHandler.GetStripes();
            var stripe = stripeKeys.FirstOrDefault(a => a.Currency == payCurrencyVO.PayCurrency);
            string clientSecret = "";
            if (payCurrencyVO.IntentType == IntentTypeEnum.PaymentIntent)
                clientSecret = CreateIntent(dbPaymentInfo, userInfo, user, bookingIds, stripe.StripeKey).ClientSecret;
            else
                clientSecret = CreateSetupIntent(dbPaymentInfo, userInfo, user, bookingIds, stripe.StripeKey).ClientSecret;
            foreach (var item in bookings)
            {
                item.PaymentId = dbPaymentInfo.Id;
                item.PayCurrency = dbPaymentInfo.Currency;
                dbPaymentInfo.BookingDtls.Add(new PaymentBookingDtl() { BookingId = item.Id, AmountInfos = item.AmountInfos });
            }

            var payment = await _paymentRepository.UpsertAsync(dbPaymentInfo);
            if (payment != null)
            {
                var temo = await _customerServiceHandler.UpdateCartInfo(bookings, userInfo);
            }
            return new ResponseModel { msg = "ok", code = 200, data = new { payCurrencyVO.IntentType, clientSecret, clientKey = stripe.ClientKey } };

        }
        private SetupIntent CreateSetupIntent(DbPaymentInfo dbPaymentInfo, DbCustomer userInfo, DbToken user, string bookingIds, string stripeKey)
        {
            SetupIntent setupIntent = _stripeServiceHandler.CreateSetupPayIntent(new PayIntentParam()
            {
                BillId = dbPaymentInfo.Id,
                PaymentIntentId = dbPaymentInfo.StripeIntentId,
                CustomerId = userInfo.StripeCustomerId
            }, bookingIds, user, stripeKey);
            dbPaymentInfo.StripeIntentId = setupIntent.Id;
            dbPaymentInfo.SetupPay = true;
            userInfo.StripeCustomerId = dbPaymentInfo.StripeCustomerId = setupIntent.CustomerId;
            dbPaymentInfo.StripeClientSecretKey = setupIntent.ClientSecret;
            return setupIntent;
        }
        private PaymentIntent CreateIntent(DbPaymentInfo dbPaymentInfo, DbCustomer userInfo, DbToken user, string bookingIds, string stripeKey)
        {
            PaymentIntent paymentIntent = _stripeServiceHandler.CreatePayIntent(dbPaymentInfo, bookingIds, user.UserId, stripeKey);
            dbPaymentInfo.StripeIntentId = paymentIntent.Id;
            dbPaymentInfo.SetupPay = false;
            userInfo.StripeCustomerId = dbPaymentInfo.StripeCustomerId = paymentIntent.CustomerId;
            dbPaymentInfo.StripeClientSecretKey = paymentIntent.ClientSecret;
            return paymentIntent;
        }
        public async Task<List<DbBooking>> PlaceBooking(List<DbBooking> cartInfos, int shopId, DbCustomer user, IntentTypeEnum intentType)
        {
            foreach (var item in cartInfos)
            {
                if (string.IsNullOrWhiteSpace(item.Id))
                    item.Id = Guid.NewGuid().ToString();
                item.Creater = user.Id;
                item.ShopId = shopId;
                item.Created = DateTime.UtcNow;
                item.Status = OrderStatusEnum.UnAccepted;
                item.IntentType = intentType;
                item.isOldCustomer = user.IsOldCustomer;
                await _bookingRepository.UpsertAsync(item);
                var booking = user.CartInfos.FirstOrDefault(a => a.Id == item.Id);
                user.CartInfos.Remove(booking);
            }
            await _customerServiceHandler.UpdateAccount(user, shopId);
            if (intentType == IntentTypeEnum.SetupIntent)
                await NewBookingNofitication(cartInfos, user);
            return cartInfos;
        }
        public async void SetupPaymentAction(string billId, string userId)
        {
            var user = await _customerRepository.GetOneAsync(a => a.Id == userId);
            var paymentInfo = await _paymentRepository.GetOneAsync(a => a.Id == billId && !a.Paid && !a.IsDeleted);
            var bookings = await _bookingRepository.GetManyAsync(a => a.PaymentId == billId);
            var bookingList = bookings.ToList();
            var countries = await _countryHandler.GetCountries(user.ShopId ?? 11);
            var dbStripes = await _countryHandler.GetStripes();
            paymentInfo.Amount = _amountCalculaterV1.CalculateOrderPaidAmount(bookingList, paymentInfo.Currency, user, countries, dbStripes);

            var stripeKeys = await _countryHandler.GetStripes();
            var stripe = stripeKeys.FirstOrDefault(a => a.Currency == paymentInfo.Currency);
            _stripeServiceHandler.SetupPaymentAction(paymentInfo, userId, stripe.StripeKey);
        }
        public async void BookingChargedOld(string billId, string ChargeId, string ReceiptUrl)
        {
            var booking = await _restaurantBookingRepository.GetOneAsync(a => a.Id == billId);
            if (booking != null)
            {
                booking.PaymentInfos[0].Paid = true;
                booking.PaymentInfos[0].StripeChargeId = ChargeId;
                booking.PaymentInfos[0].StripeReceiptUrl = ReceiptUrl;
                booking.PaymentInfos[0].PayTime = DateTime.UtcNow;
                booking.Status = OrderStatusEnum.UnAccepted;
                booking.Details.ForEach(a => a.Status = OrderStatusEnum.Accepted);
                await _restaurantBookingRepository.UpsertAsync(booking);
            }
        }
        public async void BookingCharged(string billId, string bookingIds, string ChargeId, string ReceiptUrl)
        {
            var paymentInfos = await _paymentRepository.GetOneAsync(a => a.Id == billId);
            if (paymentInfos != null)
            {
                paymentInfos.StripeChargeId = ChargeId;
                paymentInfos.StripeReceiptUrl = ReceiptUrl;
                paymentInfos.PayTime = DateTime.UtcNow;
                paymentInfos.Paid = true;
            }
            else
            {
                return;
            }

            var payment = await _paymentRepository.UpsertAsync(paymentInfos);
            if (payment != null)
            {
                DateTime now = DateTime.Now;
                List<DbBooking> bookings = new List<DbBooking>();
                var user = await _customerServiceHandler.GetCustomer(payment.Creater, payment.ShopId ?? 11);
                var books = await _bookingRepository.GetManyAsync(a => a.PaymentId == billId);
                var intentType = payment.SetupPay ? IntentTypeEnum.SetupIntent : IntentTypeEnum.PaymentIntent;
                if (books == null || books.Count() == 0)
                {
                    List<DbBooking> _bookings = user.CartInfos.FindAll(a => bookingIds.Contains(a.Id));
                    if (_bookings == null || _bookings.Count() == 0)
                    {
                        var trBooking = await _restaurantBookingRepository.GetOneAsync(a => a.Id == bookingIds);
                        _bookings = trBooking.Details;
                    }


                    books = await PlaceBooking(_bookings, payment.ShopId ?? 11, user, intentType);
                }
                bookings = books.ToList();


                foreach (var booking in bookings)
                {
                    booking.Charged = true;

                    if (!payment.SetupPay)
                    {
                        booking.AllowCancel = false;
                        booking.AllowEdit = false;
                        booking.IntentType = intentType;
                    }
                    booking.isOldCustomer = user.IsOldCustomer;
                    await _bookingRepository.UpsertAsync(booking);
                }
                if (bookings[0].IntentType != IntentTypeEnum.SetupIntent)
                    await NewBookingNofitication(bookings, user);
            }
        }
        private async Task<bool> InitBooking(TrDbRestaurantBooking booking, string userId, bool noPay)
        {
            if (string.IsNullOrWhiteSpace(booking.CustomerEmail) ||
                string.IsNullOrWhiteSpace(booking.CustomerPhone) ||
                string.IsNullOrWhiteSpace(booking.CustomerName))
            {
                DbCustomer user = await _customerRepository.GetOneAsync(a => a.Id == userId);
                booking.CustomerName = user.UserName;
                booking.CustomerPhone = user.Phone;
                booking.CustomerEmail = user.Email;

            }
            booking.Created = DateTime.UtcNow;
            foreach (var item in booking.Details)
            {
                await InitBookingDetail(item as DbBooking, userId);
                if (noPay)
                    item.Status = OrderStatusEnum.UnAccepted;
            }
            if (noPay)
            {
                booking.Status = OrderStatusEnum.UnAccepted;

            }
            return noPay;
        }
        private async Task<bool> InitBookingDetail(DbBooking item, string userId)
        {
            if (string.IsNullOrWhiteSpace(item.BookingRef))
            {
                item.BookingRef = "GM" + SnowflakeId.getSnowId();
            }

            var rest = await _restaurantRepository.GetOneAsync(a => a.Id == item.RestaurantId);
            if (rest != null)
            {

                item.RestaurantName = rest.StoreName;
                item.RestaurantEmail = rest.Email;
                item.RestaurantAddress = rest.Address;
                item.RestaurantPhone = rest.PhoneNumber;
                item.EmergencyPhone = rest.ContactPhone;
                item.RestaurantWechat = rest.Wechat;
                item.RestaurantTimeZone = rest.TimeZone;
                item.Currency = rest.Currency;
                item.RestaurantCountry = rest.Country;
                item.BillInfo = rest.BillInfo;
            }

            if (string.IsNullOrWhiteSpace(item.Id))
                item.Id = Guid.NewGuid().ToString();
            DbCustomer user = null;
            if (string.IsNullOrWhiteSpace(item.ContactEmail) && string.IsNullOrWhiteSpace(item.ContactPhone) && string.IsNullOrWhiteSpace(item.ContactName))
            {
                user = await _customerRepository.GetOneAsync(a => a.Id == userId);
                if (user != null)
                {
                    item.ContactEmail = user.Email;
                    item.ContactName = "GroupMeals " + user.UserName;
                    item.ContactPhone = user.Phone;
                    item.ContactWechat = user.WeChat;
                }
            }

            if (item.AmountInfos.Count() == 0)
            {
                if (user == null)
                    user = await _customerRepository.GetOneAsync(a => a.Id == userId);
                var amount = _amountCalculaterV1.getItemAmount(item.ConvertToAmount());
                var itemPayInfo = _amountCalculaterV1.getItemPayAmount(item.ConvertToAmount(), user, rest.Vat);
                itemPayInfo.Id = Guid.NewGuid().ToString();
                item.AmountInfos.Add(itemPayInfo);
            }

            foreach (var amount in item.AmountInfos)
            {
                if (string.IsNullOrWhiteSpace(amount.Id))
                    amount.Id = "A" + SnowflakeId.getSnowId();
            }
            return true;
        }

        private async Task<bool> CancelbookingNotification(DbBooking booking)
        {

            PushFCMMessage(new List<DbBooking>() { booking }, "Booking Canceled", "You got a canceled booking");
            _twilioUtil.sendSMS("+353874858555", $"你有订单: {booking.BookingRef}被取消。 请登录groupmeal.com查看更多");
            var shopInfo = await _shopRepository.GetOneAsync(r => r.ShopId == booking.ShopId && r.IsActive.HasValue && r.IsActive.Value);
            if (shopInfo == null)
            {
                _logger.LogError("----------------Cannot find shop info" + booking.Id);
            }
            var emailParams = EmailConfigs.Instance.Emails[EmailTypeEnum.MealCancelled];
            emailParams.isShortInfo = 1;
            await _sendEmailUtil.EmailEach(new List<DbBooking>() { booking }, shopInfo, emailParams);
            return true;
        }
        private async Task<bool> NewBookingNofitication(List<DbBooking> bookings, DbCustomer user)
        {
            if (bookings.Count == 0) return false;

            PushFCMMessage(bookings, "New Booking", "You got a new booking");

            SaveMsgPush(bookings);

            var shopInfo = await _shopRepository.GetOneAsync(r => r.ShopId == user.ShopId && r.IsActive.HasValue && r.IsActive.Value);
            if (shopInfo == null)
            {
                _logger.LogError("Cannot find shop info");
            }
            try
            {
                _twilioUtil.sendSMS("+353874858555", $"你有{bookings.Count()}条新的订单。 请登录groupmeal.com查看更多");
            }
            catch (Exception ex)
            {
                _logger.LogError("sendSMS"+ex.Message);
            }
            var emailParams = EmailConfigs.Instance.Emails[EmailTypeEnum.NewMealCustomer];
            emailParams.isShortInfo = 0;
            await _sendEmailUtil.EmailGroup(bookings, shopInfo, emailParams, user);

            emailParams = EmailConfigs.Instance.Emails[EmailTypeEnum.NewMealRestaurant];
            emailParams.isShortInfo = 1;
            await _sendEmailUtil.EmailEach(bookings, shopInfo, emailParams);
            //EmailUtils.EmailSupport(booking, shopInfo, "new_meals_support", this._environment.WebRootPath, "New Booking", _twilioUtil, _contentBuilder,  _logger);
            return true;
        }
        private async void PushFCMMessage(List<DbBooking> bookings, string title, string msgBody)
        {
            try
            {
                foreach (var item in bookings)
                {
                    string selectDateTimeStr = item.SelectDateTime.Value.GetLocaTimeByIANACode(item.RestaurantTimeZone).ToString("yyyy-MMM-dd HH:mm");
                    string Menu = item.Courses[0].MenuItemName;
                    var boss = await _customerRepository.GetOneAsync(a => a.Email == item.RestaurantEmail);
                    string token = boss.DeviceToken;
                    string body = $"{item.Courses[0].MenuItemName}*{item.Courses[0].Qty} {selectDateTimeStr}";
                    MsgTypeEnum msgTypeEnum = MsgTypeEnum.Text;
                    switch (item.Status)
                    {
                        case OrderStatusEnum.None:
                        case OrderStatusEnum.Canceled:
                        case OrderStatusEnum.UnAccepted:
                            msgTypeEnum = MsgTypeEnum.UnAcceptOrder;
                            break;
                        case OrderStatusEnum.Accepted:
                        case OrderStatusEnum.OpenOrder:
                        case OrderStatusEnum.Settled:
                        default:
                            msgTypeEnum = MsgTypeEnum.AcceptOrder;
                            break;
                    }
                    FCMMessage FCMParams = new FCMMessage()
                    {
                        Title = title,
                        Body = body,
                        ReferenceId = item.GroupRef,
                        OrderStatus = item.Status,
                        MsgType = msgTypeEnum,
                        DeviceToken = token
                    };
                    await _FCMUtil.SendMsg(FCMParams);
                    //保存到消息列表
                    await _msgPusherServiceHandler.AddMsg(new Domain.Common.PushMsgModel()
                    {
                        Id = Guid.NewGuid().ToString(),
                        MsgType = msgTypeEnum,
                        SendTime = DateTime.UtcNow,
                        Created = DateTime.UtcNow,
                        Title = title,
                        Message = body,
                        MessageReference = item.BookingRef,
                        OrderStauts = item.Status,
                        Receiver = item.RestaurantEmail,
                        Sender = "GroupMeals",
                        ShopId = item.ShopId
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("PushFCMMessage: " + ex.Message);
            }
        }
        private void SaveMsgPush(List<DbBooking> bookings)
        {
            try
            {
                foreach (var item in bookings)
                {
                    string selectDateTimeStr = item.SelectDateTime.Value.GetLocaTimeByIANACode(item.RestaurantTimeZone).ToString("yyyy-MM-dd HH:mm");
                    _msgPusherServiceHandler.AddMsg(new Domain.Common.PushMsgModel()
                    {
                        Id = Guid.NewGuid().ToString(),
                        MsgType = MsgTypeEnum.Order,
                        SendTime = DateTime.UtcNow,
                        Created = DateTime.UtcNow,
                        Title = "下单成功通知",
                        Message = $"{item.RestaurantName} {selectDateTimeStr}",
                        MessageReference = item.BookingRef,
                        Receiver = item.Creater,
                        Sender = "GroupMeals",
                        ShopId = item.ShopId
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("SaveMsgPush: " + ex.Message);
            }
        }
        private async void ModifybookingNotification(DbBooking booking)
        {
            PushFCMMessage(new List<DbBooking>() { booking }, "Booking Modified", "You got a modified booking");
            var shopInfo = await _shopRepository.GetOneAsync(r => r.ShopId == booking.ShopId && r.IsActive.HasValue && r.IsActive.Value);
            if (shopInfo == null)
            {
                throw new ServiceException("Cannot find shop info");
            }
            try
            {
                _twilioUtil.sendSMS("+353874858555", $"你有订单被修改: {booking.BookingRef}。 请登录groupmeal.com查看更多");
            }
            catch (Exception ex)
            {
            }
            var emailParams = EmailConfigs.Instance.Emails[EmailTypeEnum.MealModified];
            emailParams.isShortInfo = 1;
            await _sendEmailUtil.EmailEach(new List<DbBooking>() { booking }, shopInfo, emailParams);

            var userInfo = await _customerRepository.GetOneAsync(r => r.Id == booking.Creater);
            var emailParamsUser = EmailConfigs.Instance.Emails[EmailTypeEnum.MealModified];
            emailParamsUser.isShortInfo = 0;
            await _sendEmailUtil.EmailEach(new List<DbBooking>() { booking }, shopInfo, emailParamsUser);

        }
        public async Task<bool> DoRebate(string bookingId, double rebate)
        {

            var booking = await _bookingRepository.GetOneAsync(r => r.Id == bookingId);
            if (booking != null)
            {
                //booking.Rebate = rebate;
                await _bookingRepository.UpsertAsync(booking);
            }

            return true;
        }

        public async Task<bool> ResendEmail(string bookingId)
        {
            _logger.LogInfo("ResendEmail.bookingid:" + bookingId);

            try
            {
                DbBooking booking = await _bookingRepository.GetOneAsync(r => r.Id == bookingId);
                if (booking != null)
                {
                    _logger.LogInfo("ResendEmail.BookingRef:" + booking.BookingRef);
                    //_twilioUtil.sendSMS("+353874858555", $"你有新的订单: {booking.BookingRef} ");

                    var shopInfo = await _shopRepository.GetOneAsync(r => r.ShopId == booking.ShopId && r.IsActive.HasValue && r.IsActive.Value);
                    if (shopInfo == null)
                    {
                        throw new ServiceException("Cannot find shop info");
                    }
                    var user = await _customerRepository.GetOneAsync(a => a.Id == booking.Creater);
                    var emailParams = EmailConfigs.Instance.Emails[EmailTypeEnum.MealModified];
                    emailParams.isShortInfo = 0;
                    await _sendEmailUtil.EmailEach(new() { booking }, shopInfo, emailParams);

                    //await _sendEmailUtil.EmailGroup(new List<DbBooking>() { booking }, shopInfo, emailParams, user);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("----------------err" + ex);
            }
            return true;
        }
        public async Task<bool> SavePayKeyCustomerId(string userId, string customerId, string intentId, string secertKey)
        {
            var customer = await _customerRepository.GetOneAsync(a => a.Id == userId);
            if (customer != null && customer.CartInfos.Count > 0)
            {
                var paymentInfo = await _paymentRepository.GetOneAsync(a => a.Id == customer.CartInfos[0].PaymentId);
                if (paymentInfo != null)
                {
                    if (!string.IsNullOrWhiteSpace(secertKey))
                    {
                        paymentInfo.StripeIntentId = intentId;
                        paymentInfo.StripeCustomerId = customerId;
                        paymentInfo.StripeClientSecretKey = secertKey;
                        paymentInfo.StripeSetupIntent = true;
                    }
                    var res = await _paymentRepository.UpsertAsync(paymentInfo);
                }
                else
                {

                }

            }
            //var newItem = await _stripeCheckoutSeesionRepository.CreateAsync(new StripeCheckoutSeesion() { Data = session, BookingId = booking.Id });

            return true;
        }
        public async Task<bool> DeleteBooking(string bookingId, int shopId)
        {
            var booking = await _bookingRepository.GetOneAsync(a => a.Id == bookingId);
            booking.IsDeleted = true;
            booking.Updated = DateTime.UtcNow;
            var savedRestaurant = await _bookingRepository.UpsertAsync(booking);
            return savedRestaurant != null;
        }

        public async Task<bool> UndoDeleteDetail(string bookingId, string detailId, int shopId)
        {
            var booking = await _bookingRepository.GetOneAsync(a => a.Id == bookingId);
            booking.IsDeleted = false;
            booking.Updated = DateTime.UtcNow;
            var savedRestaurant = await _bookingRepository.UpsertAsync(booking);
            return savedRestaurant != null;
        }
        public async Task<ResponseModel> SearchBookings(int shopId, DbToken user, string content, int pageSize = -1, string continuationToken = null)
        {
            var listres = await SearchBookingsV1(shopId, user.UserId, content, pageSize, continuationToken);
            List<DbBooking> dbBookings = listres.data as List<DbBooking>;
            var aaa = getBooking(dbBookings);
            listres.data = aaa;
            return listres;

            pageSize = -1;
            //SettleOrder();
            List<TrDbRestaurantBooking> res = new List<TrDbRestaurantBooking>();
            string pageToken = "";
            if (string.IsNullOrWhiteSpace(content))
            {
                var Bookings = await _restaurantBookingRepository.GetManyAsync(a => (a.Status != OrderStatusEnum.None && !a.IsDeleted && a.CustomerEmail == user.UserEmail), pageSize, continuationToken);
                res = Bookings.Value.ToList();
                pageToken = Bookings.Key;


            }
            else
            {
                var _content = content.ToLower().Trim();
                var Bookings = await _restaurantBookingRepository.GetManyAsync(a => (a.Status != OrderStatusEnum.None && !a.IsDeleted && a.CustomerEmail == user.UserEmail &&
                (a.BookingRef.ToLower().Contains(_content) ||
                a.Details.Any(d => d.RestaurantName.ToLower().Contains(_content)))), pageSize, continuationToken);


                res = Bookings.Value.ToList();
                pageToken = Bookings.Key;

            }
            res.ForEach(r => { r.Details.OrderByDescending(d => d.SelectDateTime); });
            var list = res.OrderByDescending(a => a.Details.Max(d => d.SelectDateTime)).ToList();

            return new ResponseModel { msg = "ok", code = 200, token = pageToken, data = list };
        }
        public List<TrDbRestaurantBooking> getBooking(List<DbBooking> source)
        {

            List<TrDbRestaurantBooking> res = new List<TrDbRestaurantBooking>();
            foreach (var item in source)
            {
                if (item.BillInfo.PaymentType == PaymentTypeEnum.Percentage || item.BillInfo.PaymentType == PaymentTypeEnum.Fixed)
                {
                    item.BillInfo.PaymentType = PaymentTypeEnum.Fixed;
                }
                else
                {
                    item.BillInfo.PaymentType = PaymentTypeEnum.Full;
                }
                TrDbRestaurantBooking booking = new TrDbRestaurantBooking()
                {
                    Id = item.Id,
                    BookingRef = item.BookingRef,
                    Created = item.Created,
                    Creater = item.Creater,
                    CustomerEmail = item.ContactEmail,
                    CustomerName = item.ContactName,
                    CustomerPhone = item.ContactPhone,
                    IsActive = item.IsActive,
                    IsDeleted = item.IsDeleted,
                    PayCurrency = item.PayCurrency,
                    ShopId = item.ShopId,
                    Status = item.Status,
                    Details = new List<DbBooking>() { item }
                };
                res.Add(booking);
            }
            return res;
        }
        public async Task<ResponseModel> SearchBookingsV1(int shopId, string userId, string content, int pageSize = -1, string continuationToken = null)
        {
            pageSize = -1;
            //SettleOrder();
            List<DbBooking> res = new List<DbBooking>();
            string pageToken = "";
            if (string.IsNullOrWhiteSpace(content))
            {
                var Bookings = await _bookingRepository.GetManyAsync(a => (!a.IsDeleted && a.Creater == userId), pageSize, continuationToken);
                res = Bookings.Value.ToList();
                pageToken = Bookings.Key;


            }
            else
            {
                var _content = content.ToLower().Trim();
                var Bookings = await _bookingRepository.GetManyAsync(a => (!a.IsDeleted && a.Creater == userId &&
                (a.BookingRef.ToLower().Contains(_content) ||
                a.RestaurantName.ToLower().Contains(_content))), pageSize, continuationToken);


                res = Bookings.Value.ToList();
                pageToken = Bookings.Key;

            }
            res.OrderByDescending(d => d.SelectDateTime);
            var list = await _paymentRepository.GetManyAsync(a => 1 == 1);
            var paymentInfos = list.ToList();
            foreach (var booking in res)
            {
                UpdateForOutput(booking, paymentInfos);
            }
            return new ResponseModel { msg = "ok", code = 200, token = pageToken, data = res };

        }


        private DbBooking UpdateForOutput(DbBooking dbBooking, List<DbPaymentInfo> paymentInfos)
        {
            List<OrderStatusEnum> allowEditStatus = new List<OrderStatusEnum>() { OrderStatusEnum.UnAccepted, OrderStatusEnum.Accepted };
            if (allowEditStatus.IndexOf(dbBooking.Status) >= 0)
            {
                dbBooking.AllowCancel = true;
                dbBooking.AllowEdit = true;
            }
            if (paymentInfos != null)
            {
                var paymentInfo = paymentInfos.FirstOrDefault(a => a.Id == dbBooking.PaymentId);
                if (paymentInfo != null && !paymentInfo.SetupPay)
                {
                    //dbBooking.AllowCancel = false;
                    //dbBooking.AllowEdit = false;
                    dbBooking.IntentType = paymentInfo.SetupPay ? IntentTypeEnum.SetupIntent : IntentTypeEnum.PaymentIntent;
                }
            }
            dbBooking.ContactName = "GroupMeals_" + dbBooking.ContactName;
            return dbBooking;
        }
        public async void SettleOrder()
        {
            try
            {
                DateTime stime = DateTime.UtcNow;
                var Bookings = await _bookingRepository.GetManyAsync(a => (!a.IsDeleted));
                var span = (DateTime.UtcNow - stime).TotalMilliseconds;
                Console.WriteLine(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff") + " : " + span);
                var list = Bookings.ToList();
                foreach (var item in list)
                {
                    bool isSettled = true;
                    if (item.AcceptStatus == AcceptStatusEnum.Accepted && item.SelectDateTime < DateTime.UtcNow)
                    {

                    }
                    else
                    {
                        isSettled = false;
                    }
                    if (isSettled && item.Status != OrderStatusEnum.Settled)
                    {
                        item.Status = OrderStatusEnum.Settled;
                        item.AcceptStatus = AcceptStatusEnum.Settled;
                        await _bookingRepository.UpsertAsync(item);
                    }
                }
            }
            catch (Exception ex)
            {

                throw;
            }
        }
        #region SearchBooking
        public async Task<ResponseModel> SearchBookingsByRestaurant(int shopId, string email, string content, int filterTime, DateTime stime, DateTime etime, List<int> status, int pageSize = -1, string continuationToken = null)
        {
            Expression<Func<DbBooking, bool>> expression = null;
            Func<IQueryable<DbBooking>, IOrderedQueryable<DbBooking>> orderBy = a => a.OrderBy(b => b.SelectDateTime);
            List<DbBooking> res = new List<DbBooking>();
            string pageToken = "";
            KeyValuePair<string, IEnumerable<DbBooking>> Bookings = new KeyValuePair<string, IEnumerable<DbBooking>>();
            bool emptyTime = stime == DateTime.MinValue || etime == DateTime.MinValue;

            if (string.IsNullOrWhiteSpace(content))
            {
                if (emptyTime)
                {
                    expression = a => (a.Status != OrderStatusEnum.None && !a.IsDeleted && a.RestaurantEmail == email && status.Contains((int)a.Status));
                }
                else
                {
                    etime = etime.AddDays(1);
                    if (filterTime == 1)
                    {
                        expression = a => (a.Status != OrderStatusEnum.None && !a.IsDeleted && a.RestaurantEmail == email &&
                         a.SelectDateTime > stime && a.SelectDateTime <= etime && status.Contains((int)a.Status));
                    }
                    else
                    {
                        expression = a => (a.Status != OrderStatusEnum.None && !a.IsDeleted && a.RestaurantEmail == email &&
                        a.Created > stime && a.Created <= etime && status.Contains((int)a.Status));
                    }
                }
            }
            else
            {
                content = content.ToLower().Trim();
                expression = a => ((a.Status != OrderStatusEnum.None) && !a.IsDeleted && a.RestaurantEmail == email && status.Contains((int)a.Status) &&
                (a.BookingRef.ToLower().Contains(content) || a.RestaurantName.ToLower().Contains(content) || a.RestaurantAddress.ToLower().Contains(content) ||
                a.ContactName.ToLower().Contains(content) || a.GroupRef.ToLower().Contains(content)));
            }
            if (status.Count == 1 && status[0] == (int)OrderStatusEnum.Canceled)
                orderBy = a => a.OrderByDescending(b => b.Updated);

            Bookings = await _bookingRepository.GetManyOrderbyAsync(expression, orderBy, pageSize, continuationToken);
            res = Bookings.Value.ToList();
            pageToken = Bookings.Key;

            foreach (var booking in res)
            {
                UpdateForOutput(booking, null);
            }

            return new ResponseModel { msg = "ok", code = 200, token = pageToken, data = res };
        }

        public async Task<ResponseModel> SearchBookingsByAdmin(int shopId, string content, int filterTime, DateTime stime, DateTime etime, List<int> status, int pageSize = -1, string continuationToken = null)
        {
            Expression<Func<DbBooking, bool>> expression = null;
            Func<IQueryable<DbBooking>, IOrderedQueryable<DbBooking>> orderBy = a => a.OrderByDescending(b => b.Created);
            List<DbBooking> res = new List<DbBooking>();
            string pageToken = "";
            KeyValuePair<string, IEnumerable<DbBooking>> Bookings = new KeyValuePair<string, IEnumerable<DbBooking>>();
            bool emptyTime = stime == DateTime.MinValue || etime == DateTime.MinValue;
            if (string.IsNullOrWhiteSpace(content))
            {
                if (status[0] == 6)
                {
                    if (emptyTime)
                    {
                        expression = a => (a.Status != OrderStatusEnum.None && a.IsDeleted);

                    }
                    else
                    {
                        etime = etime.AddDays(1);
                        if (filterTime == 1)
                        {
                            expression = a => (a.Status != OrderStatusEnum.None && a.IsDeleted &&
                            a.SelectDateTime > stime && a.SelectDateTime <= etime);
                        }
                        else
                        {
                            expression = a => (a.Status != OrderStatusEnum.None && a.IsDeleted &&
                            a.Created > stime && a.Created <= etime);
                        }

                    }

                }
                else if (status[0] == -1)
                {
                    if (emptyTime)
                    {
                        expression = a => (a.Status != OrderStatusEnum.None && !a.IsDeleted);

                    }
                    else
                    {
                        etime = etime.AddDays(1);
                        if (filterTime == 1)
                        {
                            expression = a => (a.Status != OrderStatusEnum.None && !a.IsDeleted &&
                            a.SelectDateTime > stime && a.SelectDateTime <= etime);

                        }
                        else
                        {
                            expression = a => (a.Status != OrderStatusEnum.None && !a.IsDeleted &&
                            a.Created > stime && a.Created <= etime);
                        }
                    }
                }
                else
                {
                    if (emptyTime)
                    {
                        expression = a => (a.Status != OrderStatusEnum.None && a.Status == (OrderStatusEnum)status[0] && !a.IsDeleted);
                    }
                    else
                    {
                        etime = etime.AddDays(1);
                        if (filterTime == 1)
                        {
                            expression = a => (a.Status != OrderStatusEnum.None && !a.IsDeleted &&
                            a.SelectDateTime > stime && a.SelectDateTime <= etime && a.Status == (OrderStatusEnum)status[0]);
                        }
                        else
                        {
                            expression = a => (a.Status != OrderStatusEnum.None && !a.IsDeleted &&
                            a.Created > stime && a.Created <= etime && a.Status == (OrderStatusEnum)status[0]);
                        }
                    }
                }
            }
            else
            {
                content = content.ToLower().Trim();
                expression = a => ((a.Status != OrderStatusEnum.None) && !a.IsDeleted &&
                (a.BookingRef.ToLower().Contains(content) || a.RestaurantName.ToLower().Contains(content) || a.RestaurantAddress.ToLower().Contains(content) ||
                a.ContactName.ToLower().Contains(content) || a.GroupRef.ToLower().Contains(content)));
            }
            Bookings = await _bookingRepository.GetManyOrderbyAsync(expression, orderBy, pageSize, continuationToken);
            res = Bookings.Value.ToList();
            var rests = await _trRestaurantServiceHandler.GetAllRestaurants();
            pageToken = Bookings.Key;
            res.ForEach(r =>
            {
                var resRest = rests.FirstOrDefault(a => a.Id == r.RestaurantId);
                if (resRest != null)
                    r.PrivatePhone = resRest.PrivatePhone;
            });

            return new ResponseModel { msg = "ok", code = 200, token = pageToken, data = res };
        }
        #endregion

        #region AmountCalculater

        public ResponseModel GetBookingItemAmountV1(BookingCalculateVO bookingCalculateVO, PaymentTypeEnum rewardType, double reward, bool isOldCustomer, double vat)
        {
            decimal amount = 0, paidAmount = 0;
            if (bookingCalculateVO != null)
            {
                paidAmount = _amountCalculaterV1.getItemPayAmount(bookingCalculateVO, new DbCustomer() { IsOldCustomer = isOldCustomer, RewardType = rewardType, Reward = reward },
                    vat).PaidAmount;
                amount = _amountCalculaterV1.getItemAmount(bookingCalculateVO);
            }
            return new ResponseModel { msg = "ok", code = 200, data = new { amount, paidAmount } };
        }
        public ResponseModel GetBookingItemAmount(List<BookingCourse> menuItems, PaymentTypeEnum paymentType, double payRate)
        {
            if (paymentType == PaymentTypeEnum.Percentage && payRate <= 0)
                return new ResponseModel { msg = "payRate should greater than 0", code = 501 };
            decimal amount = 0, paidAmount = 0;
            BookingCalculateVO bookingCalculateVO = new BookingCalculateVO()
            {
                RestaurantIncluedVAT = false,
                BillInfo = new RestaurantBillInfo() { PaymentType = paymentType, PayRate = payRate },
                Courses = new List<MenuInfo>()
            };
            foreach (var item in menuItems)
            {
                MenuInfo info = new MenuInfo()
                {
                    ChildrenPrice = item.ChildrenPrice,
                    ChildrenQty = item.ChildrenQty,
                    MenuCalculateType = item.MenuCalculateType,
                    Price = item.Price,
                    Qty = item.Qty
                };
                bookingCalculateVO.Courses.Add(info);
            }
            if (paymentType == PaymentTypeEnum.Fixed)
                paidAmount = 0;
            else
                paidAmount = _amountCalculaterV1.getItemPayAmount(bookingCalculateVO, new DbCustomer() { IsOldCustomer = true, RewardType = PaymentTypeEnum.Full, Reward = 0 }, 0.125).PaidAmount;
            amount = _amountCalculaterV1.getItemAmount(bookingCalculateVO);
            return new ResponseModel { msg = "ok", code = 200, data = new { amount, paidAmount } };
        }
        public async Task<ResponseModel> GetBookingAmountV1(bool isBookingModify, string currency, string userId, List<string> Ids)
        {
            DateTime sdate = DateTime.UtcNow;

            DateTime stime = DateTime.UtcNow;
            var user = await _customerRepository.GetOneAsync(a => a.Id == userId);
            user = await _customerServiceHandler.RefreshCartInfo(user);
            Console.WriteLine("cart:" + (DateTime.UtcNow - stime).TotalMilliseconds);
            var details = user.CartInfos.FindAll(c => Ids.Contains(c.Id));
            if (details == null || details.Count() == 0)
            {
                return new ResponseModel { msg = "detailId can't find in cartinfo", code = 501, };
            }

            var countries = await _countryHandler.GetCountries(user.ShopId ?? 11);

            var dbStripes = await _countryHandler.GetStripes();
            var amountInfo = _amountCalculaterV1.GetOrderPaidInfo(details, currency, user.ShopId ?? 11, user, countries, dbStripes);

            Console.WriteLine("Total: " + (DateTime.UtcNow - sdate).TotalMilliseconds);
            return new ResponseModel { msg = "ok", code = 200, data = amountInfo };

        }
        private async Task<PaymentAmountInfo> GetAmountInfoByOldVersion(List<DbBooking> items, string currency, string userId)
        {
            List<DbBooking> details = new List<DbBooking>();
            foreach (var bo in items)
            {
                DbBooking booking = new DbBooking()
                {
                    BillInfo = bo.BillInfo,
                    Courses = new List<BookingCourse>(),
                    Currency = bo.Currency,
                    RestaurantIncluedVAT = false
                };
                foreach (var c in bo.Courses)
                {
                    var course = new BookingCourse()
                    {
                        ChildrenPrice = c.ChildrenPrice,
                        ChildrenQty = c.ChildrenQty,
                        MenuCalculateType = c.MenuCalculateType,
                        Price = c.Price,
                        Qty = c.Qty
                    };
                    booking.Courses.Add(course);
                }
                details.Add(bo);
            }
            var user = await _customerRepository.GetOneAsync(a => a.Id == userId);
            user = await _customerServiceHandler.RefreshCartInfo(user);
            var countries = await _countryHandler.GetCountries(user.ShopId ?? 11);
            var dbStripes = await _countryHandler.GetStripes();
            var amountInfo = _amountCalculaterV1.GetOrderPaidInfo(details, currency, user.ShopId ?? 11, user, countries, dbStripes);
            return amountInfo;
        }
        public async Task<ResponseModel> GetBookingAmount(bool isBookingModify, string currency, string userId, List<string> Ids)
        {
            decimal EUAmount = 0, UKAmount = 0, EUPaidAmount = 0, UKPaidAmount = 0, totalPayAmount = 0;
            DateTime sdate = DateTime.UtcNow;
            if (isBookingModify)
            {
                DateTime stime = DateTime.UtcNow;
                var Bookings = await _restaurantBookingRepository.GetManyAsync(a => a.Details.Any(d => Ids.Contains(d.Id)));
                Console.WriteLine((DateTime.UtcNow - stime).TotalMilliseconds);
                if (Bookings == null || Bookings.Count() == 0)
                {
                    return new ResponseModel { msg = "detailId can't find in Order list", code = 501, };
                }

                foreach (var item in Bookings)
                {
                    var items = item.Details.FindAll(d => Ids.Contains(d.Id));
                    List<DbBooking> details = new List<DbBooking>();
                    foreach (var bo in items)
                    {
                        DbBooking booking = new DbBooking()
                        {
                            BillInfo = bo.BillInfo,
                            Courses = new List<BookingCourse>(),
                            Currency = bo.Currency,
                            RestaurantIncluedVAT = false
                        };
                        foreach (var c in bo.Courses)
                        {
                            var course = new BookingCourse() { ChildrenPrice = c.ChildrenPrice, ChildrenQty = c.ChildrenQty, MenuCalculateType = c.MenuCalculateType, Price = c.Price, Qty = c.Qty };
                            booking.Courses.Add(course);
                        }
                        details.Add(bo);
                    }
                    var user = await _customerRepository.GetOneAsync(a => a.Id == userId);
                    user = await _customerServiceHandler.RefreshCartInfo(user);
                    var countries = await _countryHandler.GetCountries(user.ShopId ?? 11);
                    var dbStripes = await _countryHandler.GetStripes();
                    var amountInfo = _amountCalculaterV1.GetOrderPaidInfo(details, currency, user.ShopId ?? 11, user, countries, dbStripes);
                    totalPayAmount = amountInfo.TotalPayAmount;

                    UKAmount = 0;
                    UKPaidAmount = 0;
                }
            }
            else
            {
                DateTime stime = DateTime.UtcNow;
                var user = await _customerRepository.GetOneAsync(a => a.Id == userId);
                Console.WriteLine("cart:" + (DateTime.UtcNow - stime).TotalMilliseconds);
                var items = user.CartInfos.FindAll(c => Ids.Contains(c.Id));
                if (items == null || items.Count() == 0)
                {
                    return new ResponseModel { msg = "detailId can't find in cartinfo", code = 501, };

                }

                var countries = await _countryHandler.GetCountries(user.ShopId ?? 11);
                List<DbBooking> details = new List<DbBooking>();
                foreach (var bo in items)
                {
                    DbBooking booking = new DbBooking()
                    {
                        BillInfo = bo.BillInfo,
                        Courses = new List<BookingCourse>(),
                        Currency = bo.Currency,
                        RestaurantIncluedVAT = false
                    };
                    foreach (var c in bo.Courses)
                    {
                        var course = new BookingCourse() { ChildrenPrice = c.ChildrenPrice, ChildrenQty = c.ChildrenQty, MenuCalculateType = c.MenuCalculateType, Price = c.Price, Qty = c.Qty };
                        booking.Courses.Add(course);
                    }
                    details.Add(bo);
                }
                user = await _customerServiceHandler.RefreshCartInfo(user);
                var dbStripes = await _countryHandler.GetStripes();
                var amountInfo = _amountCalculaterV1.GetOrderPaidInfo(details, currency, user.ShopId ?? 11, user, countries, dbStripes);
                totalPayAmount = amountInfo.TotalPayAmount;
                List<string> amountStr = amountInfo.AmountText.Split(" + ").ToList();
                foreach (var item in amountStr)
                {
                    decimal amount = 0;

                    if (item.StartsWith("CHF"))
                    {
                        decimal.TryParse(item.Substring(4, item.Length - 4), out amount);
                        EUAmount += amount;
                    }
                    if (item.StartsWith("€"))
                    {
                        decimal.TryParse(item.Substring(2, item.Length - 2), out amount);
                        EUAmount += amount;
                    }
                    if (item.StartsWith("£"))
                    {
                        decimal.TryParse(item.Substring(2, item.Length - 2), out amount);
                        UKAmount += amount;
                    }
                }
                List<string> paidAmountStr = amountInfo.UnPaidAmountText.Split(" + ").ToList();
                foreach (var item in paidAmountStr)
                {
                    decimal amount = 0;

                    if (item.StartsWith("CHF"))
                    {
                        decimal.TryParse(item.Substring(4, item.Length - 4), out amount);
                        EUPaidAmount += amount;
                    }
                    if (item.StartsWith("€"))
                    {
                        decimal.TryParse(item.Substring(2, item.Length - 2), out amount);
                        EUPaidAmount += amount;
                    }
                    if (item.StartsWith("£"))
                    {
                        decimal.TryParse(item.Substring(2, item.Length - 2), out amount);
                        UKPaidAmount += amount;
                    }
                }
            }
            EUPaidAmount = EUAmount - EUPaidAmount;
            UKPaidAmount = UKAmount - UKPaidAmount;
            Console.WriteLine("Total: " + (DateTime.UtcNow - sdate).TotalMilliseconds);
            return new ResponseModel { msg = "ok", code = 200, data = new { EUAmount, UKAmount, EUPaidAmount, UKPaidAmount, totalPayAmount } };
        }
        private string JionDictionary(Dictionary<string, decimal> dicAmount, List<DbCountry> Countries)
        {
            List<string> temp = new List<string>();
            foreach (var item in dicAmount)
            {
                string symbol = "";
                var country = Countries.FirstOrDefault(c => c.Name == item.Key);
                if (country != null)
                {
                    symbol = country.CurrencySymbol;
                }
                temp.Add($"{symbol} {item.Value}");
            }
            string amountText = string.Join(" + ", temp);
            return amountText;
        }
        #endregion

        public async Task<ResponseModel> GetSchedulePdf(DbToken userId)
        {
            List<PDFModel> pdfData = new List<PDFModel>();
            var test = await _bookingRepository.GetManyAsync(a => 1 == 1);
            var Bookings = await _bookingRepository.GetManyAsync(a => a.Status != OrderStatusEnum.None &&
        !a.IsDeleted && a.Status != OrderStatusEnum.Canceled && a.Status != OrderStatusEnum.Settled && a.Creater == userId.UserId);
            foreach (var Booking in Bookings)
            {
                if (Booking.IsDeleted || Booking.Status == OrderStatusEnum.Canceled || Booking.Status == OrderStatusEnum.Settled) continue;
                Console.WriteLine(Booking.BookingRef + "." + Booking.Status.ToString() + " : " + Booking.AcceptStatus.ToString());
                if ((int)Booking.AcceptStatus > 1) continue;//只加入待接单与已接单的
                string selectDateTimeStr = Booking.SelectDateTime.Value.GetLocaTimeByIANACode(Booking.RestaurantTimeZone).ToString("yyyy-MM-dd HH:mm:ss");
                string mealStr = "";
                foreach (var meal in Booking.Courses)
                {
                    mealStr += $"{meal.MenuItemName}({meal.Price})*{meal.Qty + meal.ChildrenQty}{Environment.NewLine}";
                }
                decimal amount = Booking.AmountInfos.Sum(a => a.Amount);
                string currencyStr = Booking.Currency == "" ? "" : "";
                string mealInfo = $"{mealStr}{currencyStr} ";
                var model = new PDFModel
                {
                    mealInfo = mealInfo,
                    BookingRef = Booking.BookingRef,
                    BookingTime = selectDateTimeStr,
                    RestuarantName = Booking.RestaurantName,
                    Phone = Booking.RestaurantPhone,
                    ContactPhone = Booking.EmergencyPhone,
                    Email = Booking.ContactEmail,
                    Wechat = Booking.ContactWechat,
                    Remark = Booking.Memo,
                    Address = Booking.RestaurantAddress
                };
                pdfData.Add(model);
            }

            var doc = _pDFUtil.GeneratePdf(pdfData);
            System.IO.Stream fileStream = new MemoryStream(doc.GeneratePdf());
            string fileName = $"PDFs/订单行程单{DateTime.UtcNow.ToString("yyyyMMddHHmmssfff")}.pdf";

            bool saveSucc = await ImageUploader.UploadFileToStorage(fileStream, fileName, storageConfig);
            if (saveSucc)
            {
                var url = ImageUploader.GetImageUrl(storageConfig) + fileName;

                return new ResponseModel { msg = "ok", code = 200, data = url };
            }
            else
                return new ResponseModel { msg = "保存失败", code = 501, };

        }
        public async Task<ResponseModel> ExportBooking()
        {

            List<DbBooking> Bookings = new List<DbBooking>();
            string token = "";
            while (token != null)
            {
                var temo = await _bookingRepository.GetManyAsync(a => (a.Status != OrderStatusEnum.None && !a.IsDeleted), 500, token);
                var list = temo.Value.ToList();
                Bookings.AddRange(list);
                token = temo.Key;
            }

            //var userstet = await _bookingRepository.GetManyAsync(a => a.Creater==null);

            var users = await _customerRepository.GetManyAsync(a => 1 == 1);
            var payments = await _paymentRepository.GetManyAsync(a => 1 == 1);
            //Bookings = await GetDbBookings();
            var csv = BookingExportBuilder(Bookings, users.ToList(), payments.ToList());

            return new ResponseModel { msg = "ok", code = 200, data = csv };

        }
        private async Task<List<DbBooking>> GetDbBookings()
        {
            List<DbBooking> bookings = new List<DbBooking>();
            List<DbBooking> res = new List<DbBooking>();
            string token = "";
            while (token != null)
            {
                var temo = await _bookingRepository.GetManyAsync(a => (a.Charged == false && a.PaymentId != null), 900, token);
                var list = temo.Value.ToList();
                bookings.AddRange(list);
                token = temo.Key;
            }
            List<DbPaymentInfo> pays = new List<DbPaymentInfo>();
            token = "";
            while (token != null)
            {
                var temo = await _paymentRepository.GetManyAsync(a => (1 == 1), 500, token);
                var paylist = temo.Value.ToList();
                pays.AddRange(paylist);
                token = temo.Key;
            }
            pays = pays.FindAll(a => a.SetupPay);
            foreach (var item in pays)
            {
                var booo = bookings.FirstOrDefault(a => a.PaymentId == item.Id);
                if (booo == null)
                { }
                else
                { }

            }

            foreach (var item in bookings)
            {
                if (string.IsNullOrWhiteSpace(item.PaymentId))
                {

                    continue;
                }
                var pay = pays.FirstOrDefault(a => a.Id == item.PaymentId);
                if (pay != null && pay.Paid)
                {
                    item.Charged = true;
                    await _bookingRepository.UpsertAsync(item);
                }

                if (pay == null && item.isOldCustomer == false)
                {
                    res.Add(item);
                }
            }
            return res;
        }

        private List<BookingExportModel> BookingExportBuilder(List<DbBooking> data, List<DbCustomer> users, List<DbPaymentInfo> payments)
        {
            List<BookingExportModel> bookings = new List<BookingExportModel>();

            foreach (var item in data)
            {

                var menu = "";
                var qty = 0;
                var price = 0m;
                item.Courses.ForEach(a =>
                {
                    menu += a.MenuItemName;
                    qty += a.Qty;
                    price = a.Price;
                });
                bool isNewCustomer = false;
                var user = users.FirstOrDefault(a => a.Id == item.Creater);
                if (user != null)
                {
                    isNewCustomer = !user.IsOldCustomer;
                }
                BookingExportModel model = new BookingExportModel()
                {
                    RestaurantName = item.RestaurantName,
                    RestaurantPhone = item.RestaurantPhone,
                    RestaurantCountry = item.RestaurantCountry,
                    EmergencyPhone = item.EmergencyPhone,
                    RestaurantWechat = item.RestaurantWechat,
                    RestaurantEmail = item.RestaurantEmail,
                    RestaurantAddress = item.RestaurantAddress,
                    ContactName = item.ContactName,
                    ContactPhone = item.ContactPhone,
                    ContactEmail = item.ContactEmail,
                    ContactWechat = item.ContactWechat,
                    GroupRef = item.GroupRef,
                    BookingRef = item.BookingRef,
                    MealDate = item.SelectDateTime.Value.ToString("yyyy-MM-dd"),
                    MealTimeStr = item.SelectDateTime.Value.ToString("HH:mm"),
                    CreateDate = item.Created.Value.ToString("yyyy-MM-dd"),
                    CreateTime = item.Created.Value.ToString("HH:mm"),
                    CreaterName = user?.UserName,
                    CreaterEmail = user?.Email,
                    IsNewCustomer = isNewCustomer,
                    PayCurrency = item.PayCurrency ?? item.Currency,
                    Amount = item.AmountInfos.Sum(a => a.Amount),
                    Unpaid = item.AmountInfos.Sum(a => a.Unpaid),
                    MenuStr = menu,
                    StatusStr = item.Status.GetEnumDescription(),
                    Qty = qty.ToString(),
                    Price = price.ToString(),
                    Reward = item.AmountInfos.Sum(a => a.Reward),
                    Memo = item.Memo,
                    Remark = item.Remark
                };
                bookings.Add(model);
            }
            return bookings;

        }
        public async Task<ResponseModel> GetDashboardData()
        {

            List<DbBooking> Bookings = new List<DbBooking>();
            string token = "";
            while (token != null)
            {
                var temo = await _bookingRepository.GetManyAsync(a => (a.Status != OrderStatusEnum.None), 500, token);
                var list = temo.Value.ToList();
                Bookings.AddRange(list);
                token = temo.Key;
            }

            int totalQty = Bookings.Count();
            var monthBooking = Bookings.Where(a => a.Created > DateTime.UtcNow.AddDays(1 - DateTime.UtcNow.Day).Date).ToList();
            int monthQty = monthBooking.Count();
            var weekBooking = Bookings.Where(a => a.Created > DateTime.UtcNow.Date.AddDays(-7)).ToList();
            int todayQty = weekBooking.Where(a => a.Created > DateTime.UtcNow.Date).ToList().Count;
            int week = weekBooking.Count();
            List<BookingReport> weekly = new List<BookingReport>();

            weekly.Add(new BookingReport() { Name = DateTime.UtcNow.ToString("yyyy-MM-dd") + " (" + todayQty + ")", Qty = todayQty });
            int weekQty = 0;
            for (int i = 1; i < 7; i++)
            {
                weekQty = weekly.Sum(a => a.Qty);
                int dayqty = weekBooking.Where(a => a.Created > DateTime.UtcNow.Date.AddDays(-i)).ToList().Count - weekQty;
                weekly.Add(new BookingReport() { Name = DateTime.UtcNow.Date.AddDays(-i).ToString("yyyy-MM-dd") + " (" + dayqty + ")", Qty = dayqty });
            }
            weekly.Reverse();
            return new ResponseModel { msg = "ok", code = 200, data = new { totalQty, todayQty, weekQty, monthQty, weekly } };
        }
        public async Task<bool> OrderCheck()
        {
            try
            {
                Task.Run(() =>
            {
                try
                {
                    autoPayment();
                }
                catch (Exception ex)
                {
                    _logger.LogError("autoPayment:" + ex.Message);
                }
            });

                Task.Run(() =>
                {
                    try
                    { RefereshBookinginfoToUser(); }
                    catch (Exception ex)
                    {
                        _logger.LogError("RefereshBookinginfoToUser: " + ex.Message);
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError("OrderCheck:" + ex.Message);
            }


            //return true;

            ////updateRest();
            //return true;

            //return true;
            //asyncBooking();
            //return true;

            //ExportBooking();
            //return true;
            //asyncCustomer();
            //return true;
            //asyncCities();
            //return true;
            try
            {
                var today = DateTime.UtcNow;

                if (today.Hour == 4 && today.Minute < 15)
                {
                    _exchangeUtil.UpdateExchangeRateToDB();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("UpdateExchangeRateToDB:" + ex.Message);
            }
            //DateTime time = DateTime.UtcNow.GetLocaTimeByIANACode(_dateTimeUtil.GetIANACode("Ireland"));
            //if (time.Hour < 18 && time.Hour > 8)
            //{
            //    var bookings = await _bookingRepository.GetManyAsync(a => a.Details.Any(d=>d.Status == OrderStatusEnum.UnAccepted ));
            //    if (bookings != null && bookings.Count() > 0)
            //    {
            //        var unAcceptOrderCount = bookings.Count(a => (DateTime.UtcNow - a.Created.Value).TotalMinutes > 30);
            //        if (unAcceptOrderCount > 0)
            //        {
            //            _logger.LogInfo($"你有[{unAcceptOrderCount}]张订单超过30分钟未接单，请登录groupmeal.com查看更多");
            //            _twilioUtil.sendSMS("+353874858555", $"你有[{bookings.Count()}]张订单超过30分钟未接单，请登录groupmeal.com查看更多");
            //        }
            //    }
            //}
            return true;
        }
        #region AutoFunctions

        public async void autoPayment()
        {
            try
            {


                var bookings = await _bookingRepository.GetManyAsync(a => a.IntentType == IntentTypeEnum.SetupIntent && a.Status == OrderStatusEnum.Settled && a.Charged == false);
                foreach (var booking in bookings)
                {
                    var paymentInfo = await _paymentRepository.GetOneAsync(a => !a.Paid && a.Id == booking.PaymentId);
                    if (paymentInfo == null || string.IsNullOrWhiteSpace(paymentInfo.StripePaymentMethodId))
                        continue;

                    PayAction(paymentInfo, booking);
                    //Thread.Sleep(500000);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("autoPayment:" + ex.Message);
            }
        }
        private async void PayAction(DbPaymentInfo item, DbBooking booking, bool byadmin = false)
        {

            //if (item.PaymentType == 0)
            {
                var bookings = await _bookingRepository.GetManyAsync(a => a.PaymentId == item.Id);
                bool isTimePass = true;
                bool isAllSettle = true;
                if (booking.SelectDateTime.Value > DateTime.UtcNow)
                {
                    isTimePass = false;
                }
                if (booking.Status != OrderStatusEnum.Settled)
                    isAllSettle = false;
                if ((byadmin && isAllSettle) || isTimePass)
                {
                    SetupPaymentAction(item.Id, item.Creater);
                }
            }
            //else
            //{
            //    if ((DateTime.UtcNow - item.CheckoutTime).TotalHours > 24)
            //    {
            //        SetupPaymentAction(item.Id, item.Creater);
            //    }
            //}
        }
        public async Task<List<DbBooking>> GetAllBookings(bool refreshCache = false)
        {
            var restaurants = new List<DbBooking>();
            var cacheKey = string.Format("motionmedia-All{0}", typeof(DbBooking).Name);
            if (refreshCache)
                _memoryCache.Set<List<DbBooking>>(cacheKey, null);
            var cacheResult = _memoryCache.Get<List<DbBooking>>(cacheKey);
            if (cacheResult != null && cacheResult.Count() > 0)
            {
                return restaurants;
            }
            string token = "";
            while (token != null)
            {
                var res = await _bookingRepository.GetManyAsync(a => (1 == 1), 1000, token);
                restaurants.AddRange(res.Value);
                token = res.Key;
            }
            return restaurants;

        }
        private async void RefereshBookinginfoToUser()
        {

            var customers = await _customerServiceHandler.GetAllUsers(true);
            List<DbBooking> bookings = await GetAllBookings(true);
            List<TrDbRestaurant> restaurants = await _trRestaurantServiceHandler.GetAllRestaurants(true);//  new List<TrDbRestaurant>();

            foreach (var item in customers)
            {
                int change = 0;
                int count = 0;
                count = bookings.ToList().FindAll(a => a.RestaurantEmail.ToLower().Trim() == item.Email.ToLower().Trim()).Count;

                if (item.IsBoss)
                {
                    if (item.BookingQty != count)
                        change++;
                    item.BookingQty = count;
                }
                else
                {
                    count = bookings.ToList().FindAll(a => a.Creater == item.Id).Count;
                    if (item.BookingQty != count)
                        change++;
                    item.BookingQty = count;
                }

                var rests = restaurants.FindAll(a => { if (a.Users != null) return a.Users.Contains(item.Email); else return false; });
                if (rests != null)
                {
                    List<string> reststrs = new List<string>();
                    foreach (var rest in rests)
                    {
                        if (!reststrs.Contains(rest.StoreName))
                            reststrs.Add(rest.StoreName);
                    }
                    string rts = string.Join(",\r\n", reststrs);
                    if (item.Restaurants != rts)
                        change++;
                    item.Restaurants = rts;
                }
                if (change == 0)
                    continue;
                await _customerRepository.UpsertAsync(item);
                Thread.Sleep(50);
            }

        }
        public async void asyncBooking()
        {

            //var temo = await _bookingRepository.GetManyAsync(a => 1 == 1);
            //var len = temo.ToList().Count;
            //var bookings = await _restaurantBookingRepository.GetManyAsync(a => a.Created<new DateTime(2024,6,4,18,30,36));

            List<TrDbRestaurantBooking> bookings = new List<TrDbRestaurantBooking>();
            string token = "";
            while (token != null)
            {
                var temo = await _restaurantBookingRepository.GetManyAsync(a => (a.Status != OrderStatusEnum.None && a.Created > new DateTime(2024, 11, 7, 18, 30, 36)), 500, token);
                var list = temo.Value.ToList();
                bookings.AddRange(list);
                token = temo.Key;
            }

            var rests = await _restaurantRepository.GetManyAsync(a => a.BillInfo.SupportedPaymentTypes.Count == 0);

            foreach (var item in rests)
            {
                if (item.BillInfo.PaymentType == PaymentTypeEnum.Full)
                {
                    item.BillInfo.SupportedPaymentTypes = new List<PaymentTypeEnum>() { PaymentTypeEnum.Full };
                }
                else
                    item.BillInfo.SupportedPaymentTypes = new List<PaymentTypeEnum>() { PaymentTypeEnum.Fixed };
            }


            var temooo = await _bookingRepository.GetManyAsync(a => a.RestaurantId == null);

            var users = await _customerRepository.GetManyAsync(a => a.ShopId == 11);
            int i = 0;
            foreach (var booking in bookings)
            {

                //if (i > 300) break;
                foreach (var book in booking.Details)
                {
                    string paymentId = Guid.NewGuid().ToString();
                    if (booking.IsDeleted || book.Status == OrderStatusEnum.Canceled || book.Status == OrderStatusEnum.None)
                    { }
                    else
                        foreach (var pay in booking.PaymentInfos)
                        {
                            DbPaymentInfo paymentInfo = new DbPaymentInfo()
                            {
                                Id = paymentId,
                                Creater = book.Creater,
                                Amount = pay.Amount,
                                Paid = pay.Paid,
                                PaymentType = pay.PaymentType,
                                SetupPay = pay.SetupPay,
                                StripeChargeId = pay.StripeChargeId,
                                StripeClientSecretKey = pay.StripeClientSecretKey,
                                StripeCustomerId = pay.StripeCustomerId,
                                StripePaymentMethodId = pay.StripePaymentMethodId,
                                StripePriceId = pay.StripePriceId,
                                StripeProductId = pay.StripeProductId,
                                StripeReceiptUrl = pay.StripeReceiptUrl,
                                StripeSetupIntent = pay.StripeSetupIntent,
                                ShopId = booking.ShopId,
                                Created = booking.Created,

                            };
                            await _paymentRepository.UpsertAsync(paymentInfo);
                        }
                    var cust = users.FirstOrDefault(a => a.Email == booking.Creater);
                    DbBooking dbBooking = new DbBooking()
                    {
                        Id = book.Id,
                        PaymentId = paymentId,
                        AcceptReason = book.AcceptReason,
                        AcceptStatus = book.AcceptStatus,
                        BookingRef = booking.BookingRef,
                        Courses = book.Courses,
                        Created = booking.Created,
                        Currency = book.Currency,
                        GroupRef = book.GroupRef,
                        IsDeleted = booking.IsDeleted,
                        SelectDateTime = book.SelectDateTime,
                        Memo = book.Memo,
                        ShopId = booking.ShopId,
                        Remark = book.Remark,
                        Status = book.Status,
                        AmountInfos = book.AmountInfos,
                        Creater = cust?.Id

                    };
                    dbBooking.ContactEmail = book.ContactEmail;
                    dbBooking.ContactWechat = book.ContactWechat;
                    dbBooking.ContactPhone = book.ContactPhone;
                    dbBooking.ContactName = book.ContactName;
                    dbBooking.ContactInfos = book.ContactInfos;


                    var rest = rests.FirstOrDefault(a => a.Id == book.RestaurantId);
                    if (rest != null)
                    {
                        dbBooking.RestaurantId = rest.Id;
                        dbBooking.RestaurantName = rest.StoreName;
                        dbBooking.RestaurantEmail = rest.Email;
                        dbBooking.RestaurantAddress = rest.Address;
                        dbBooking.RestaurantPhone = rest.PhoneNumber;
                        dbBooking.EmergencyPhone = rest.ContactPhone;
                        dbBooking.RestaurantWechat = rest.Wechat;
                        dbBooking.RestaurantTimeZone = rest.TimeZone;
                        dbBooking.Currency = rest.Currency;
                        dbBooking.RestaurantCountry = rest.Country;
                    }
                    dbBooking.BillInfo = book.BillInfo;
                    i++;
                    await _bookingRepository.UpsertAsync(dbBooking);
                    Console.WriteLine(i);
                    //continue;
                    //foreach (var pay in booking.Operations)
                    //{
                    //    DbOpearationInfo dbOpearationInfo = new DbOpearationInfo()
                    //    {
                    //        Id = Guid.NewGuid().ToString(),
                    //        Created = booking.Created,
                    //        ModifyInfos = pay.ModifyInfos,
                    //        ModifyType = pay.ModifyType,
                    //        Operater = pay.Operater,
                    //        Operation = pay.Operation,
                    //        ShopId = booking.ShopId,
                    //        ReferenceId = book.Id,
                    //    };
                    //    await _opearationRepository.UpsertAsync(dbOpearationInfo);
                    //}

                }

            }

        }
        private async void updateRest()
        {
            var rests = await _restaurantRepository.GetManyAsync(a => a.ShopId == 11);
            var countrys = await _countryHandler.GetCountries(11);
            foreach (var item in rests)
            {
                var count = countrys.FirstOrDefault(a => a.Code == item.Country);
                if (count == null)
                {
                    continue;
                }
                var city = count.Cities.FirstOrDefault(a => item.City.Contains(a.Name));
                if (city == null)
                {
                    continue;
                }
                item.City = city.Name;
                item.TimeZone = city.TimeZone;
                item.Currency = count.Currency;
                item.Vat = count.VAT;


                //if (item.BillInfo.SupportedPaymentTypes.Count == 1)
                //{
                //    if (item.BillInfo.SupportedPaymentTypes[0] == PaymentTypeEnum.Fixed)
                //    {
                //        item.BillInfo.PaymentType = PaymentTypeEnum.Full;
                //        item.BillInfo.RewardType = PaymentTypeEnum.Full;
                //        item.IncluedVAT = false;
                //        item.ShowPaid = false;
                //    }
                //    else
                //    {

                //    }

                //}
                //else
                //{
                //    item.BillInfo.PaymentType = PaymentTypeEnum.Percentage;
                //    item.BillInfo.PayRate = 0;
                //    item.BillInfo.RewardType = PaymentTypeEnum.Full;
                //    item.BillInfo.Reward = 0;
                //    item.ShowPaid = true;
                //}

                await _restaurantRepository.UpsertAsync(item);
            }



        }


        #endregion
    }

}
