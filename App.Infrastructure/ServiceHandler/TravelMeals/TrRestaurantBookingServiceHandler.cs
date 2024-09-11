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
using Twilio.TwiML.Voice;
using Microsoft.AspNetCore.Http.Metadata;
using Stripe.Terminal;
using App.Domain.TravelMeals.VO;
using Microsoft.CodeAnalysis;

namespace App.Infrastructure.ServiceHandler.TravelMeals
{
    public interface ITrRestaurantBookingServiceHandler
    {
        Task<DbBooking> GetBooking(string id);
        Task<bool> UpdateBooking(string billId, string productId, string priceId);
        Task<bool> BookingPaid(string bookingId, string customerId = "", string chargeId = "", string payMethodId = "", string receiptUrl = "");
        Task<bool> SavePayKeyCustomerId(string userId, string customerId, string intentId, string secertKey);
        Task<bool> ResendEmail(string bookingId);
        Task<bool> OrderCheck();
        Task<bool> DoRebate(string bookingId, double rebate);
        Task<ResponseModel> GetDashboardData();
        Task<ResponseModel> GetSchedulePdf(DbToken userId);
        Task<object> UpdateAccepted(string bookingId, string subBillId, int acceptType, string operater);
        Task<bool> UpdateAcceptedReason(string bookingId, string subBillId, string reason, string operater);
        Task<object> CancelBooking(string bookingId, string detailId, string userEmail, bool isAdmin);
        Task<object> SettleBooking(string bookingId, string detailId, string userEmail);
        Task<object> UpdateStatusByAdmin(string id, int status, DbToken user);

        Task<object> UpsetBookingRemark(string bookingId, string detailId, string remark, string userEmail);
        Task<ResponseModel> MakeABooking(PayCurrencyVO booking, int shopId, DbToken user);
        Task<ResponseModel> ModifyBooking(DbBooking booking, int shopId, string email, bool isNotify = true);
        Task<bool> DeleteBooking(string bookingId, int shopId);
        Task<bool> UndoDeleteDetail(string bookingId, string detailId, int shopId);
        Task<ResponseModel> SearchBookings(int shopId, string userId, string content, int pageSize = -1, string continuationToke = null);
        Task<ResponseModel> SearchBookingsByRestaurant(int shopId, string email, string content, int pageSize = -1, string continuationToke = null);
        Task<ResponseModel> SearchBookingsByAdmin(int shopId, string content, int filterTime, DateTime stime, DateTime etime, int status, int pageSize = -1, string continuationToke = null);
        List<DbBooking> PlaceBooking(List<DbBooking> cartInfos, int shopId, DbCustomer user);

        ResponseModel GetBookingItemAmount(List<BookingCourse> menuItems, PaymentTypeEnum paymentType, double payRate, bool isOldCustomer);
        Task<ResponseModel> GetBookingAmount(bool isBookingModify, string currency, string userId, List<string> Ids);
        void SetupPaymentAction(string billId, string userId);
        void BookingCharged(string billId, string ChargeId, string ReceiptUrl);
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
        IStripeServiceHandler _stripeServiceHandler;

        private readonly ICountryServiceHandler _countryHandler;
        private readonly IShopServiceHandler _shopServiceHandler;
        private readonly IContentBuilder _contentBuilder;
        ISendEmailUtil _sendEmailUtil;
        ILogManager _logger;
        ITwilioUtil _twilioUtil;
        IStripeUtil _stripeUtil;
        IHostingEnvironment _environment;
        private readonly IDbCommonRepository<DbShop> _shopRepository;
        private readonly ICustomerServiceHandler _customerServiceHandler;
        private readonly IExchangeUtil _exchangeUtil;
        IMemoryCache _memoryCache;
        private readonly IDateTimeUtil _dateTimeUtil;
        IAmountCalculaterUtil _amountCalculaterV1;
        IPDFUtil _pDFUtil;
        private readonly AzureStorageConfig storageConfig;

        public TrRestaurantBookingServiceHandler(ITwilioUtil twilioUtil, IDbCommonRepository<TrDbRestaurant> restaurantRepository, IDbCommonRepository<TrDbRestaurantBooking> restaurantBookingRepository,
            IDbCommonRepository<DbCustomer> customerRepository,
            IDbCommonRepository<DbShop> shopRepository, ICustomerServiceHandler customerServiceHandler, IHostingEnvironment environment, IStripeUtil stripeUtil, IMemoryCache memoryCache,
            IShopServiceHandler shopServiceHandler, IStripeServiceHandler stripeServiceHandler,
        ICountryServiceHandler countryHandler, IDbCommonRepository<StripeCheckoutSeesion> stripeCheckoutSeesionRepository, IDateTimeUtil dateTimeUtil, IAmountCalculaterUtil amountCalculaterV1,
        ISendEmailUtil sendEmailUtil, IExchangeUtil exchangeUtil, Microsoft.Extensions.Options.IOptions<AzureStorageConfig> _storageConfig,
    IDbCommonRepository<DbBooking> bookingRepository, IDbCommonRepository<DbPaymentInfo> paymentRepository, IDbCommonRepository<DbOpearationInfo> opearationRepository,
        IPDFUtil pDFUtil, ILogManager logger, IContentBuilder contentBuilder)
        {
            _restaurantRepository = restaurantRepository;
            _restaurantBookingRepository = restaurantBookingRepository;
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
            _logger = logger;
            _pDFUtil = pDFUtil;
            _twilioUtil = twilioUtil;
            _environment = environment;
            _shopRepository = shopRepository;
            _customerServiceHandler = customerServiceHandler;
            _stripeUtil = stripeUtil;
            _memoryCache = memoryCache;
            _dateTimeUtil = dateTimeUtil;
            _amountCalculaterV1 = amountCalculaterV1;
            _shopServiceHandler = shopServiceHandler;
        }


        public async Task<DbBooking> GetBooking(string id)
        {
            var Booking = await _bookingRepository.GetOneAsync(r => r.Id == id);
            return Booking;
        }

        public async Task<object> SettleBooking(string bookingId, string detailId, string userEmail)
        {
            var booking = await _bookingRepository.GetOneAsync(a => a.Id == bookingId);
            booking.AcceptStatus = AcceptStatusEnum.SettledByAdmin;
            booking.Status = OrderStatusEnum.Settled;
            await _bookingRepository.UpsertAsync(booking);
            var paymentInfo = await _paymentRepository.GetOneAsync(a => a.Id == booking.PaymentId);
            PayAction(paymentInfo);
            return new { code = 0, msg = "ok", };
        }
        public async Task<object> UpsetBookingRemark(string bookingId, string detailId, string remark, string userEmail)
        {
            var booking = await _bookingRepository.GetOneAsync(a => a.Id == bookingId);
            booking.Remark = remark;
            booking.Updater = userEmail;
            booking.Updated = DateTime.UtcNow;
            await _bookingRepository.UpsertAsync(booking);
            return new { code = 0, msg = "ok", };
        }
        public async Task<object> CancelBooking(string bookingId, string detailId, string userEmail, bool isAdmin)
        {//Europe/Dublin Europe/London Europe/Paris


            var booking = await _bookingRepository.GetOneAsync(a => a.Id == bookingId);
            if (booking.Status == OrderStatusEnum.Canceled)
                return new { code = 0, msg = "订单已取消", };
            else
            {

                //if (!isAdmin && (item.SelectDateTime.Value.GetLocaTimeByIANACode(_dateTimeUtil.GetIANACode(item.RestaurantCountry)) - DateTime.UtcNow.GetLocaTimeByIANACode(_dateTimeUtil.GetIANACode(item.RestaurantCountry))).TotalHours < 24)
                //{
                //return new { code = 0, msg = "距离用餐时间24小时内取消请联系客服人员：微信：groupmeals", };
                //}
            }
            booking.Status = OrderStatusEnum.Canceled;
            if (booking.AcceptStatus == AcceptStatusEnum.Declined) { }//已拒绝不作反应
            else if (booking.AcceptStatus == AcceptStatusEnum.Accepted)
                booking.AcceptStatus = AcceptStatusEnum.CanceledAfterAccepted;
            else
                booking.AcceptStatus = AcceptStatusEnum.CanceledBeforeAccepted;
            var shopInfo = await _shopRepository.GetOneAsync(r => r.ShopId == booking.ShopId && r.IsActive.HasValue && r.IsActive.Value);
            if (shopInfo == null)
            {
                _logger.LogInfo("----------------Cannot find shop info" + booking.Id);
                throw new ServiceException("Cannot find shop info");
            }
            _twilioUtil.sendSMS("+353874858555", $"你有订单: {booking.BookingRef}被取消。 请登录groupmeal.com查看更多");
            var emailParams = EmailConfigs.Instance.Emails[EmailTypeEnum.MealCancelled];
            _sendEmailUtil.SendCancelEmail(shopInfo, booking, _environment.WebRootPath, emailParams.TemplateName, emailParams.Subject, emailParams.CCEmail.ToArray());
            booking.Status = OrderStatusEnum.Canceled;
            booking.Updated = DateTime.UtcNow;
            booking.Updater = userEmail;
            //OperationInfo operationInfo = new OperationInfo() { ModifyType = 3, Operater = userEmail, UpdateTime = DateTime.UtcNow, Operation = "订单取消" };
            //booking.Operations.Add(operationInfo);

            var savedRestaurant = await _bookingRepository.UpsertAsync(booking);

            return new { code = 0, msg = "ok", };

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
        public async Task<object> UpdateStatusByAdmin(string id, int status, DbToken user)
        {
            DbBooking booking = await GetBooking(id);
            AcceptStatusEnum statusEnum = (AcceptStatusEnum)status;

            UpdateStatus(booking, status);
            var temp = await _bookingRepository.UpsertAsync(booking);
            if (temp != null)
            {

                var opt = new DbOpearationInfo() { Id = Guid.NewGuid().ToString(), Operater = user.UserId, Operation = statusEnum.ToString(), UpdateTime = DateTime.UtcNow };
                await _opearationRepository.UpsertAsync(opt);
            }
            return new ResponseModel { msg = "ok", code = 200, data = new { } };
        }
        public async Task<object> UpdateAccepted(string billId, string subBillId, int acceptType, string operater)
        {
            DbBooking booking = await _bookingRepository.GetOneAsync(a => a.Id == billId);
            if (booking.IsDeleted)
                return new { code = 1, msg = "Order Deleted(无效操作，订单已删除)", };
            switch (booking.AcceptStatus)
            {
                case AcceptStatusEnum.UnAccepted:
                    UpdateStatus(booking, acceptType);
                    break;
                case AcceptStatusEnum.Accepted:
                    var customer = await _customerRepository.GetOneAsync(a => a.Email == operater);
                    if (customer != null)
                    {
                        bool IsAdmin = customer.AuthValue.AuthVerify(8);
                        if (IsAdmin)
                        {
                            UpdateStatus(booking, acceptType);
                        }
                        else
                        {
                            return new { code = 1, msg = "Order Accepted(订单已接受，如需修改请联系客服)", };
                        }
                    }

                    break;
                case AcceptStatusEnum.Declined:
                    customer = await _customerRepository.GetOneAsync(a => a.Email == operater);
                    if (customer != null)
                    {
                        bool IsAdmin = customer.AuthValue.AuthVerify(8);
                        if (IsAdmin)
                        {
                            UpdateStatus(booking, acceptType);
                        }
                        else
                        {
                            return new { code = 2, msg = "Order Declined(订单已被拒绝，如需修改请联系客服)", };
                        }
                    }

                    break;
                case AcceptStatusEnum.CanceledBeforeAccepted:
                case AcceptStatusEnum.CanceledAfterAccepted:
                    customer = await _customerRepository.GetOneAsync(a => a.Email == operater);
                    if (customer != null)
                    {
                        bool IsAdmin = customer.AuthValue.AuthVerify(8);
                        if (IsAdmin)
                        {
                            UpdateStatus(booking, acceptType);
                        }
                        else
                        {
                            return new { code = 2, msg = "Order Declined(订单已取消，如需修改请联系客服)", };
                        }
                    }



                    break;
                case AcceptStatusEnum.Settled:
                case AcceptStatusEnum.SettledByAdmin:
                    return new { code = 2, msg = "Order Settled(已结单，如需修改请联系客服)", };
                default:
                    break;
            }

            if (booking == null) return false;
            //if (acceptType == 2)
            //{
            //    _stripeUtil.RefundGroupMeals(booking);
            //}
            DbOpearationInfo opt = new DbOpearationInfo() { Id = Guid.NewGuid().ToString(), Operater = operater, Operation = acceptType == 1 ? "接收预订" : "拒绝预订", UpdateTime = DateTime.UtcNow };
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
                return new { code = 500, msg = "Cannot find shop info", };
            }

            SendEamilByUpdateAccept(acceptType, booking, shopInfo);
            return new { code = 0, msg = "ok", data = booking };
        }
        private void SendEamilByUpdateAccept(int acceptType, DbBooking booking, DbShop shopInfo)
        {
            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    //System.Threading.Thread.Sleep(1000 * 60);
                    if (acceptType == 1)
                    {
                        var emailParams = EmailConfigs.Instance.Emails[EmailTypeEnum.MealAccepted];
                        _sendEmailUtil?.EmailCustomer(booking, shopInfo, emailParams.TemplateName, _environment.WebRootPath, emailParams.Subject);

                        var emailParamsRest = EmailConfigs.Instance.Emails[EmailTypeEnum.MealAcceptedRestaurant];
                        var sendBooking = JsonConvert.DeserializeObject<TrDbRestaurantBooking>(JsonConvert.SerializeObject(booking));
                        sendBooking.Details.Clear();
                        List<DbBooking> bookings = new List<DbBooking>() { booking };
                        _sendEmailUtil.EmailBoss(bookings, shopInfo, emailParamsRest.TemplateName, this._environment.WebRootPath, emailParamsRest.Subject);

                    }
                    else if (acceptType == 2)
                    {
                        var emailParams = EmailConfigs.Instance.Emails[EmailTypeEnum.MealDeclined];
                        _sendEmailUtil?.EmailCustomer(booking, shopInfo, emailParams.TemplateName, this._environment.WebRootPath, emailParams.Subject);
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
            if (booking == null) return false;
            var temp = await _bookingRepository.UpsertAsync(booking);
            if (temp != null)
            {
                var opt = new DbOpearationInfo() { Operater = operater, Operation = "添加原因", UpdateTime = DateTime.UtcNow };
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
                //var emailParams = EmailConfigs.Instance.Emails[EmailTypeEnum.NewMealCustomer];
                //_sendEmailUtil.EmailCustomerTotal(booking, shopInfo, emailParams.TemplateName, this._environment.WebRootPath, emailParams.Subject);
                //emailParams = EmailConfigs.Instance.Emails[EmailTypeEnum.NewMealRestaurant];
                //_sendEmailUtil.EmailBoss(booking, shopInfo, emailParams.TemplateName, this._environment.WebRootPath, emailParams.Subject);
            }
            catch (Exception ex)
            {
                _logger.LogInfo("----------------BookingPaid.err" + ex.Message + "： " + ex.StackTrace);
            }
            return true;
        }
        private async void ClearCart(string userId, int shopId)
        {

            var customer = await _customerServiceHandler.GetCustomer(userId, shopId);
            if ((customer != null))
            {
                customer.CartInfos.Clear();

            }
        }

        /// <summary>
        /// 线下支付时：直接修改金额并发邮件通知
        /// 线上支付时：新增一条支付差价记录
        /// </summary>
        /// <param name="booking"></param>
        /// <param name="shopId"></param>
        /// <param name="email"></param>
        /// <returns></returns>
        public async Task<ResponseModel> ModifyBooking(DbBooking newBooking, int shopId, string email, bool isNotify = true)
        {
            Guard.NotNull(newBooking);
            Guard.AreEqual(newBooking.ShopId.Value, shopId);
            if (newBooking.Charged)
                return new ResponseModel { msg = "订单已捐款，不可再修改", code = 500, data = null };
            var dbBooking = await _bookingRepository.GetOneAsync(r => !r.IsDeleted && r.Id == newBooking.Id);
            if (dbBooking == null) return new ResponseModel { msg = "booking not found", code = 200, data = null };
            DbOpearationInfo operationInfo = new DbOpearationInfo() { Id = Guid.NewGuid().ToString(), ModifyType = 4, Operater = email, UpdateTime = DateTime.UtcNow, Operation = "订单修改" };
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
                if (res) isChange++;
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
                if (res)
                {
                    var rest = await _restaurantRepository.GetOneAsync(a => a.Id == newBooking.RestaurantId);
                    newBooking.RestaurantName = rest.StoreName;
                    newBooking.RestaurantEmail = rest.Email;
                    newBooking.RestaurantAddress = rest.Address;
                    newBooking.RestaurantPhone = rest.PhoneNumber;
                    newBooking.EmergencyPhone = rest.ContactPhone;
                    newBooking.RestaurantWechat = rest.Wechat;
                    isChange++;
                }

                res = UpdateField(operationInfo, dbBooking, newBooking, "RestaurantName", false);
                if (res) isChange++;
                res = UpdateField(operationInfo, dbBooking, newBooking, "RestaurantEmail", false);
                if (res) isChange++;
                res = UpdateField(operationInfo, dbBooking, newBooking, "RestaurantAddress", false);
                if (res) isChange++;
                res = UpdateField(operationInfo, dbBooking, newBooking, "RestaurantPhone", false);
                if (res) isChange++;
                res = UpdateField(operationInfo, dbBooking, newBooking, "EmergencyPhone", false);
                if (res) isChange++;
                res = UpdateField(operationInfo, dbBooking, newBooking, "RestaurantWechat", false);
                if (res) isChange++;
                var Oldamount = _amountCalculaterV1.getItemAmount(dbBooking);
                var amount = _amountCalculaterV1.getItemAmount(newBooking);
                var oldPayAmount = _amountCalculaterV1.getItemPayAmount(dbBooking);
                var payAmount = _amountCalculaterV1.getItemPayAmount(newBooking);
                if (amount != Oldamount)
                {
                    isChange++;
                    UpdateListField(operationInfo, dbBooking, newBooking, "Courses");
                    dbBooking.Courses = newBooking.Courses;

                    AmountInfo amountInfo = new AmountInfo() { Id = Guid.NewGuid().ToString() };
                    amountInfo.Amount = amount - Oldamount;//新增差价记录
                    amountInfo.PaidAmount = payAmount - oldPayAmount;
                    dbBooking.AmountInfos.Add(amountInfo);
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
                    SendModifyEmail(savedBooking);
            }
            return new ResponseModel { msg = "", code = 200, data = null };
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
                        modifyInfo.oldValue = oldValue?.ToString();
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

        public async Task<ResponseModel> MakeABooking(PayCurrencyVO payCurrencyVO, int shopId, DbToken user)
        {
            Guard.NotNull(payCurrencyVO.BookingIds);
            _logger.LogInfo("RequestBooking" + user.UserEmail);
            var userInfo = await _customerRepository.GetOneAsync(r => r.Id == user.UserId && r.CartInfos.Count() > 0);
            if (userInfo == null)
            {
                return new ResponseModel { msg = "购物车为空", code = 500, };
            }
            else
            {
                string currency = "eur";
                string payCurrency = payCurrencyVO.PayCurrency.Trim().ToLower();
                switch (payCurrency)
                {
                    case "eu":
                        currency = "eur";
                        break;
                    case "uk":
                        currency = "gbp";
                        break;
                    default:
                        break;
                }
                List<DbBooking> bookings = new List<DbBooking>();

                foreach (var item in userInfo.CartInfos)
                {
                    if (payCurrencyVO.BookingIds.Count > 0 && !payCurrencyVO.BookingIds.Contains(item.Id)) continue;
                    await InitBookingDetail(item, user.UserId);
                    item.BillInfo.IsOldCustomer = userInfo.IsOldCustomer;
                    bookings.Add(item);
                }
                DbPaymentInfo dbPaymentInfo = await _paymentRepository.GetOneAsync(a => a.Id == userInfo.CartInfos[0].PaymentId);
                if (dbPaymentInfo == null)
                {
                    dbPaymentInfo = new DbPaymentInfo();
                    dbPaymentInfo.Id = Guid.NewGuid().ToString();
                    dbPaymentInfo.Creater = userInfo.Id;
                }
                dbPaymentInfo.Amount = _amountCalculaterV1.CalculateOrderPaidAmount(bookings, payCurrencyVO.PayCurrency, userInfo);
                dbPaymentInfo.Currency = currency;

                if (dbPaymentInfo.Amount == 0)
                {
                    var dbUser = await _customerServiceHandler.UpdateCart(bookings, user.UserId, user.ShopId ?? 11);
                    var booking = PlaceBooking(bookings, shopId, userInfo);
                    return new ResponseModel { msg = "ok", code = 200, data = null };

                }

                SetupIntent setupIntent = _stripeServiceHandler.CreateSetupPayIntent(new PayIntentParam()
                {
                    BillId = dbPaymentInfo.Id,
                    PaymentIntentId = dbPaymentInfo.StripeIntentId,
                    CustomerId = userInfo.StripeCustomerId
                }, user);
                dbPaymentInfo.StripeIntentId = setupIntent.Id;
                userInfo.StripeCustomerId = dbPaymentInfo.StripeCustomerId = setupIntent.CustomerId;
                dbPaymentInfo.StripeClientSecretKey = setupIntent.ClientSecret;

                foreach (var item in bookings)
                {
                    item.PaymentId = dbPaymentInfo.Id;
                    item.PayCurrency = payCurrency;
                }

                var payment = await _paymentRepository.UpsertAsync(dbPaymentInfo);
                if (payment != null)
                {
                    var temo = await _customerServiceHandler.UpdateCart(bookings, user.UserId, user.ShopId ?? 11);

                }
                return new ResponseModel { msg = "ok", code = 200, data = setupIntent.ClientSecret };
            }
            return new ResponseModel { msg = "ok", code = 200, data = null };
        }
        public List<DbBooking> PlaceBooking(List<DbBooking> cartInfos, int shopId, DbCustomer user)
        {

            foreach (var item in cartInfos)
            {
                if (string.IsNullOrWhiteSpace(item.Id))
                    item.Id = Guid.NewGuid().ToString();
                item.Creater = user.Id;
                item.ShopId = shopId;
                item.Created = DateTime.UtcNow;
                _bookingRepository.UpsertAsync(item);
                var booking = user.CartInfos.FirstOrDefault(a => a.Id == item.Id);
                user.CartInfos.Remove(booking);
            }
            _customerServiceHandler.UpdateAccount(user, shopId);
            SendEmail(cartInfos, shopId, user);
            return cartInfos;
        }
        public async void SetupPaymentAction(string billId, string userId)
        {
            var user = await _customerServiceHandler.GetCustomer(userId, 11);
            var paymentInfo = await _paymentRepository.GetOneAsync(a => a.Id == billId);
            var bookings = await _bookingRepository.GetManyAsync(a => a.PaymentId == billId);
            var bookingList = bookings.ToList();
            paymentInfo.Amount = _amountCalculaterV1.CalculateOrderPaidAmount(bookingList, paymentInfo.Currency, user);
            _stripeServiceHandler.SetupPaymentAction(paymentInfo, userId);
        }
        public async void BookingCharged(string billId, string ChargeId, string ReceiptUrl)
        {
            var paymentInfos = await _paymentRepository.GetOneAsync(a => a.Id == billId);
            if (paymentInfos != null)
            {
                paymentInfos.StripeChargeId = ChargeId;
                paymentInfos.StripeReceiptUrl = ReceiptUrl;
                paymentInfos.PayTime = DateTime.UtcNow;
                paymentInfos.Paid = true;
            }
            var payment = await _paymentRepository.UpsertAsync(paymentInfos);
            if (payment != null)
            {
                var bookings = await _bookingRepository.GetManyAsync(a => a.PaymentId == billId);
                foreach (var booking in bookings)
                {
                    booking.Charged = true;
                    await _bookingRepository.UpsertAsync(booking);
                }
            }
        }
        private async Task<bool> InitBooking(TrDbRestaurantBooking booking, string userId)
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
            bool noPay = true;
            foreach (var item in booking.Details)
            {
                await InitBookingDetail(item as DbBooking, userId);
                //if (item.BillInfo.PaymentType != PaymentTypeEnum.Fixed)
                //    noPay = false;

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
                item.Currency = rest.Country;
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
                    item.ContactName = user.UserName;
                    item.ContactPhone = user.Phone;
                    item.ContactWechat = user.WeChat;
                }
            }

            if (item.AmountInfos.Count() == 0)
            {
                if (user == null)
                    user = await _customerRepository.GetOneAsync(a => a.Id == userId);
                AmountInfo amountInfo = new AmountInfo()
                {
                    Id = Guid.NewGuid().ToString(),
                    Amount = _amountCalculaterV1.getItemAmount(item),
                    PaidAmount = _amountCalculaterV1.getItemPayAmount(item),
                    Reward = _amountCalculaterV1.GetReward(item, user)
                };
                item.AmountInfos.Add(amountInfo);
            }

            foreach (var amount in item.AmountInfos)
            {
                if (string.IsNullOrWhiteSpace(amount.Id))
                    amount.Id = "A" + SnowflakeId.getSnowId();
            }

            return true;
        }
        private async void SendEmail(List<DbBooking> bookings, int shopId, DbCustomer user)
        {
            var shopInfo = await _shopRepository.GetOneAsync(r => r.ShopId == shopId && r.IsActive.HasValue && r.IsActive.Value);
            if (shopInfo == null)
            {
                throw new ServiceException("Cannot find shop info");
            }
            try
            {
                _twilioUtil.sendSMS("+353874858555", $"你有{bookings.Count()}条新的订单。 请登录groupmeal.com查看更多");
            }
            catch (Exception ex)
            {
            }
            var emailParams = EmailConfigs.Instance.Emails[EmailTypeEnum.NewMealCustomer];
            _sendEmailUtil.EmailCustomerTotal(bookings, shopInfo, emailParams.TemplateName, this._environment.WebRootPath, emailParams.Subject, user);
            emailParams = EmailConfigs.Instance.Emails[EmailTypeEnum.NewMealRestaurant];
            _sendEmailUtil.EmailBoss(bookings, shopInfo, emailParams.TemplateName, this._environment.WebRootPath, emailParams.Subject);
            //EmailUtils.EmailSupport(booking, shopInfo, "new_meals_support", this._environment.WebRootPath, "New Booking", _twilioUtil, _contentBuilder,  _logger);

        }
        private async void SendModifyEmail(DbBooking booking)
        {
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
            var emailParams = EmailConfigs.Instance.Emails[EmailTypeEnum.MealModified_V2];
            var user = await _customerServiceHandler.GetCustomer(booking.Creater, booking.ShopId ?? 11);
            _sendEmailUtil.EmailCustomerTotal(new List<DbBooking>() { booking }, shopInfo, emailParams.TemplateName, this._environment.WebRootPath, emailParams.Subject, user);

            //if (booking.Details[0].Status == OrderStatusEnum.UnAccepted)//订单未接收时重发接收邮件
            //{
            //    EmailUtils.EmailBoss(booking, shopInfo, emailParams.TemplateName, this._environment.WebRootPath, emailParams.Subject);
            //}
            //else

            _sendEmailUtil.SendModifiedEmail(booking, shopInfo, emailParams.TemplateName, this._environment.WebRootPath, emailParams.Subject);

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
                    var emailParams = EmailConfigs.Instance.Emails[EmailTypeEnum.NewMealRestaurant];
                    _sendEmailUtil.EmailBoss(new List<DbBooking>() { booking }, shopInfo, emailParams.TemplateName, this._environment.WebRootPath, emailParams.Subject);
                    //_sendEmailUtil.EmailCustomerTotal(booking, shopInfo, "new_meals", this._environment.WebRootPath, "New Booking", _contentBuilder,  _logger);
                    //EmailUtils.EmailBoss(booking, shopInfo, "new Order", this._environment.WebRootPath, _twilioUtil, _contentBuilder, _logger);
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
        public async Task<ResponseModel> SearchBookings(int shopId, string userId, string content, int pageSize = -1, string continuationToken = null)
        {
            pageSize = -1;
            //SettleOrder();
            List<DbBooking> res = new List<DbBooking>();
            string pageToken = "";
            if (string.IsNullOrWhiteSpace(content))
            {
                var Bookings = await _bookingRepository.GetManyAsync(a => (a.Status != OrderStatusEnum.None && !a.IsDeleted && a.Creater == userId), pageSize, continuationToken);
                res = Bookings.Value.ToList();
                pageToken = Bookings.Key;


            }
            else
            {
                var _content = content.ToLower().Trim();
                var Bookings = await _bookingRepository.GetManyAsync(a => (a.Status != OrderStatusEnum.None && !a.IsDeleted && a.Creater == userId &&
                (a.BookingRef.ToLower().Contains(_content) ||
                a.RestaurantName.ToLower().Contains(_content))), pageSize, continuationToken);


                res = Bookings.Value.ToList();
                pageToken = Bookings.Key;

            }
            res.OrderByDescending(d => d.SelectDateTime);

            return new ResponseModel { msg = "ok", code = 200, token = pageToken, data = res };

        }
        public async void SettleOrder()
        {
            try
            {
                DateTime stime = DateTime.UtcNow;
                var Bookings = await _bookingRepository.GetManyAsync(a => (a.Status != OrderStatusEnum.None && !a.IsDeleted));
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

        public async Task<ResponseModel> SearchBookingsByRestaurant(int shopId, string email, string content, int pageSize = -1, string continuationToken = null)
        {
            List<DbBooking> list = new List<DbBooking>();
            string token = "";

            if (string.IsNullOrWhiteSpace(content))
            {
                var Bookings = await _bookingRepository.GetManyAsync(a => (a.Status != OrderStatusEnum.None && !a.IsDeleted || a.RestaurantEmail == email), pageSize, continuationToken);
                list = Bookings.Value.ToList();
                token = Bookings.Key;
            }

            else
            {
                var Bookings = await _bookingRepository.GetManyAsync(a => (a.Status != OrderStatusEnum.None && !a.IsDeleted || a.RestaurantEmail == email), pageSize, continuationToken);
                list = Bookings.Value.ToList();
                token = Bookings.Key;
            }




            return new ResponseModel { msg = "ok", code = 200, token = token, data = list };
        }

        public async Task<ResponseModel> SearchBookingsByAdmin(int shopId, string content, int filterTime, DateTime stime, DateTime etime, int status, int pageSize = -1, string continuationToken = null)
        {
            List<DbBooking> res = new List<DbBooking>();
            string pageToken = "";
            KeyValuePair<string, IEnumerable<DbBooking>> Bookings = new KeyValuePair<string, IEnumerable<DbBooking>>();
            bool emptyTime = stime == DateTime.MinValue || etime == DateTime.MinValue;
            if (string.IsNullOrWhiteSpace(content))
            {
                if (status == 6)
                {
                    if (emptyTime)
                    {
                        Bookings = await _bookingRepository.GetManyAsync(a => (a.Status != OrderStatusEnum.None && a.IsDeleted), pageSize, continuationToken);
                        res = Bookings.Value.ToList();
                    }
                    else
                    {
                        etime = etime.AddDays(1);
                        if (filterTime == 1)
                        {
                            Bookings = await _bookingRepository.GetManyAsync(a => (a.Status != OrderStatusEnum.None && a.IsDeleted &&
                            a.SelectDateTime > stime && a.SelectDateTime <= etime), pageSize, continuationToken);
                            res = Bookings.Value.ToList();

                        }
                        else
                        {
                            Bookings = await _bookingRepository.GetManyAsync(a => (a.Status != OrderStatusEnum.None && a.IsDeleted &&
                            a.Created > stime && a.Created <= etime), pageSize, continuationToken);
                            res = Bookings.Value.ToList();
                        }

                    }

                }
                else if (status == -1)
                {
                    if (emptyTime)
                    {
                        Bookings = await _bookingRepository.GetManyAsync(a => (a.Status != OrderStatusEnum.None && !a.IsDeleted), pageSize, continuationToken);
                        res = Bookings.Value.ToList();
                    }
                    else
                    {
                        etime = etime.AddDays(1);
                        if (filterTime == 1)
                        {
                            Bookings = await _bookingRepository.GetManyAsync(a => (a.Status != OrderStatusEnum.None && !a.IsDeleted &&
                            a.SelectDateTime > stime && a.SelectDateTime <= etime), pageSize, continuationToken);
                            res = Bookings.Value.ToList();

                        }
                        else
                        {
                            Bookings = await _bookingRepository.GetManyAsync(a => (a.Status != OrderStatusEnum.None && !a.IsDeleted &&
                            a.Created > stime && a.Created <= etime), pageSize, continuationToken);
                            res = Bookings.Value.ToList();
                        }
                    }
                }
                else
                {
                    if (emptyTime)
                    {
                        Bookings = await _bookingRepository.GetManyAsync(a => (a.Status != OrderStatusEnum.None && !a.IsDeleted &&
                        a.SelectDateTime > stime && a.SelectDateTime <= etime), pageSize, continuationToken);
                        res = Bookings.Value.ToList();

                    }
                    else
                    {
                        etime = etime.AddDays(1);
                        if (filterTime == 1)
                        {
                            Bookings = await _bookingRepository.GetManyAsync(a => (a.Status != OrderStatusEnum.None && !a.IsDeleted &&
                            a.SelectDateTime > stime && a.SelectDateTime <= etime && a.Status == (OrderStatusEnum)status), pageSize, continuationToken);
                            res = Bookings.Value.ToList();

                        }
                        else
                        {
                            Bookings = await _bookingRepository.GetManyAsync(a => (a.Status != OrderStatusEnum.None && !a.IsDeleted &&
                            a.Created > stime && a.Created <= etime && a.Status == (OrderStatusEnum)status), pageSize, continuationToken);
                            res = Bookings.Value.ToList();

                        }
                    }

                }


            }
            else
            {
                content = content.ToLower().Trim();
                Predicate<DbBooking> predicate =
                    d => d.RestaurantName.ToLower().Contains(content) || d.RestaurantAddress.ToLower().Contains(content) || d.ContactName.ToLower().Contains(content) || d.GroupRef.ToLower().Contains(content);
                Bookings = await _bookingRepository.GetManyAsync(a => ((a.Status != OrderStatusEnum.None) &&
                (a.BookingRef.ToLower().Contains(content) || a.RestaurantName.ToLower().Contains(content) || a.RestaurantAddress.ToLower().Contains(content) ||
                a.ContactName.ToLower().Contains(content) || a.GroupRef.ToLower().Contains(content))), pageSize, continuationToken);

                res = Bookings.Value.ToList();

            }


            pageToken = Bookings.Key;
            //res.ForEach(r => { r.Details.OrderByDescending(d => d.SelectDateTime); });
            //var list = res.OrderByDescending(a => a.Created).ToList();

            return new ResponseModel { msg = "ok", code = 200, token = pageToken, data = res };
        }

        public ResponseModel GetBookingItemAmount(List<BookingCourse> menuItems, PaymentTypeEnum paymentType, double payRate, bool isOldCustomer)
        {
            decimal amount = 0, paidAmount = 0;
            DbBooking detail = new DbBooking() { Courses = menuItems, BillInfo = new RestaurantBillInfo() { IsOldCustomer = isOldCustomer, PaymentType = paymentType, PayRate = payRate } };
            paidAmount = _amountCalculaterV1.getItemPayAmount(detail);
            amount = _amountCalculaterV1.getItemAmount(detail);
            return new ResponseModel { msg = "ok", code = 200, data = new { amount, paidAmount } };
        }
        public async Task<ResponseModel> GetBookingAmount(bool isBookingModify, string currency, string userId, List<string> Ids)
        {
            DateTime sdate = DateTime.UtcNow;

            DateTime stime = DateTime.UtcNow;
            var user = await _customerRepository.GetOneAsync(a => a.Id == userId);
            Console.WriteLine("cart:" + (DateTime.UtcNow - stime).TotalMilliseconds);
            var details = user.CartInfos.FindAll(c => Ids.Contains(c.Id));
            if (details == null || details.Count() == 0)
            {
                return new ResponseModel { msg = "detailId can't find in cartinfo", code = 500, };
            }

            var countries = await _countryHandler.GetCountry(user.ShopId ?? 11);

            var amountInfo = _amountCalculaterV1.GetOrderPaidInfo(details, currency, user.ShopId ?? 11, user, countries);

            Console.WriteLine("Total: " + (DateTime.UtcNow - sdate).TotalMilliseconds);
            return new ResponseModel { msg = "ok", code = 200, data = amountInfo };

        }
        private string JionDictionary(Dictionary<string, decimal> dicAmount, DbCountry dbCountry)
        {
            List<string> temp = new List<string>();
            foreach (var item in dicAmount)
            {
                string symbol = "";
                var country = dbCountry.Countries.FirstOrDefault(c => c.Name == item.Key);
                if (country != null)
                {
                    symbol = country.CurrencySymbol;
                }
                temp.Add($"{symbol} {item.Value}");
            }
            string amountText = string.Join(" + ", temp);
            return amountText;
        }
        public async Task<ResponseModel> GetSchedulePdf(DbToken userId)
        {
            List<PDFModel> pdfData = new List<PDFModel>();
            var Bookings = await _bookingRepository.GetManyAsync(a => (a.Status != OrderStatusEnum.None && !a.IsDeleted &&
        a.IsDeleted && a.Status != OrderStatusEnum.Canceled && a.Status != OrderStatusEnum.Settled && a.Creater == userId.UserId));
            foreach (var Booking in Bookings)
            {
                if (Booking.IsDeleted || Booking.Status == OrderStatusEnum.Canceled || Booking.Status == OrderStatusEnum.Settled) continue;
                Console.WriteLine(Booking.BookingRef + "." + Booking.Status.ToString() + " : " + Booking.AcceptStatus.ToString());
                if ((int)Booking.AcceptStatus > 1) continue;//只加入待接单与已接单的
                string selectDateTimeStr = Booking.SelectDateTime.Value.GetLocaTimeByIANACode(_dateTimeUtil.GetIANACode(Booking.RestaurantCountry ?? "UK")).ToString("yyyy-MM-dd HH:mm:ss");
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
                return new ResponseModel { msg = "保存失败", code = 200, };

        }
        public async Task<ResponseModel> GetDashboardData()
        {
            var Bookings = await _bookingRepository.GetManyAsync(a => (a.Status != OrderStatusEnum.None));
            int totalQty =   Bookings.Count();
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
            autoPayment();
            var today = DateTime.UtcNow;

            if (today.Hour == 4 && today.Minute < 15)
            {
                _exchangeUtil.UpdateExchangeRateToDB();
            }
            //asyncBooking();
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
        public async void autoPayment()
        {
            try
            {
                var paymentInfos = await _paymentRepository.GetManyAsync(a => a.Paid == false);
                foreach (var item in paymentInfos)
                {
                    if (string.IsNullOrWhiteSpace(item.StripePaymentMethodId))
                        continue;
                    PayAction(item);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("autoPayment:" + ex.Message);
            }
        }
        private async void PayAction(DbPaymentInfo item)
        {

            if (item.PaymentType == 0)
            {
                var bookings = await _bookingRepository.GetManyAsync(a => a.PaymentId == item.Id);
                bool isTimePass = true;
                foreach (var booking in bookings)
                {
                    if (booking.SelectDateTime.Value > DateTime.UtcNow)
                    {
                        isTimePass = false;
                    }
                }
                if (isTimePass)
                {
                    SetupPaymentAction(item.Id, item.Creater);
                }
            }
            else
            {
                if ((DateTime.UtcNow - item.CheckoutTime).TotalHours > 24)
                {
                    SetupPaymentAction(item.Id, item.Creater);
                }
            }
        }
        public async void asyncBooking()
        {
            var bookings = await _restaurantBookingRepository.GetManyAsync(a => 1 == 1);
            foreach (var booking in bookings)
            {
                foreach (var book in booking.Details)
                {
                    string paymentId = Guid.NewGuid().ToString();
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
                    DbBooking dbBooking = new DbBooking()
                    {
                        Id = booking.Id,
                        PaymentId = paymentId,
                        AcceptReason = book.AcceptReason,
                        AcceptStatus = book.AcceptStatus,
                        BookingRef = book.BookingRef,
                        Courses = book.Courses,
                        Created = booking.Created,
                        Currency = book.Currency,
                        GroupRef = booking.BookingRef,
                        IsDeleted = booking.IsDeleted,
                        SelectDateTime = book.SelectDateTime,
                        Memo = book.Memo,
                        ShopId = booking.ShopId,
                        Remark = book.Remark,
                        Status = book.Status,

                    };
                    dbBooking.ContactEmail = book.ContactEmail;
                    dbBooking.ContactWechat = book.ContactWechat;
                    dbBooking.ContactPhone = book.ContactPhone;
                    dbBooking.ContactName = book.ContactName;
                    dbBooking.ContactInfos = book.ContactInfos;

                    dbBooking.RestaurantId = book.RestaurantId;
                    dbBooking.RestaurantName = book.RestaurantName;
                    dbBooking.RestaurantAddress = book.RestaurantAddress;
                    dbBooking.RestaurantPhone = book.RestaurantPhone;
                    dbBooking.RestaurantEmail = book.RestaurantEmail;
                    dbBooking.EmergencyPhone = book.EmergencyPhone;
                    dbBooking.RestaurantWechat = book.RestaurantWechat;
                    dbBooking.RestaurantCountry = book.RestaurantCountry;
                    await _bookingRepository.UpsertAsync(dbBooking);

                    foreach (var pay in booking.Operations)
                    {
                        DbOpearationInfo dbOpearationInfo = new DbOpearationInfo()
                        {
                            Id = Guid.NewGuid().ToString(),
                            Created = booking.Created,
                            ModifyInfos = pay.ModifyInfos,
                            ModifyType = pay.ModifyType,
                            Operater = pay.Operater,
                            Operation = pay.Operation,
                            ShopId = booking.ShopId,
                            ReferenceId = book.Id,
                        };
                        await _opearationRepository.UpsertAsync(dbOpearationInfo);
                    }

                }

            }

        }
    }
}
