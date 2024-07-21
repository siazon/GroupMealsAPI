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

namespace App.Infrastructure.ServiceHandler.TravelMeals
{
    public interface ITrRestaurantBookingServiceHandler
    {
        Task<TrDbRestaurantBooking> GetBooking(string id);
        Task<bool> UpdateBooking(string billId, string productId, string priceId);
        Task<bool> BookingPaid(Stripe.Checkout.Session session);
        Task<bool> BookingPaid(string bookingId, string customerId = "", string chargeId = "", string payMethodId = "", string receiptUrl = "");
        Task<bool> UpdateStripeClientKey(string bookingId, string paymentId, string customerId, string secertKey);
        Task<TrDbRestaurantBooking> BindingPayInfoToTourBooking(TrDbRestaurantBooking gpBooking, string PaymentId, string stripeClientSecretKey, bool isSetupPay);
        Task<bool> ResendEmail(string bookingId);
        Task<bool> OrderCheck();
        Task<bool> DoRebate(string bookingId, double rebate);
        Task<ResponseModel> GetDashboardData();
        Task<ResponseModel> GetSchedulePdf(DbToken userId);
        Task<object> UpdateAccepted(string bookingId, string subBillId, int acceptType, string operater);
        Task<bool> UpdateAcceptedReason(string bookingId, string subBillId, string reason, string operater);
        Task<object> CancelBooking(string bookingId, string detailId, string userEmail, bool isAdmin);
        Task<object> SettleBooking(string bookingId, string detailId, string userEmail);
        Task<object> UpdateStatusByAdmin(string id, string dtlId, int status, DbToken user);

        Task<object> UpsetBookingRemark(string bookingId, string detailId, string remark, string userEmail);
        Task<ResponseModel> MakeABooking(TrDbRestaurantBooking booking, int shopId, DbToken user);
        Task<ResponseModel> ModifyBooking(TrDbRestaurantBooking booking, int shopId, string email, bool isNotify = true);
        Task<bool> DeleteBooking(string bookingId, int shopId);
        Task<bool> DeleteBookingDetail(string bookingId, string detailId, int shopId);
        Task<bool> UndoDeleteDetail(string bookingId, string detailId, int shopId);
        Task<ResponseModel> SearchBookings(int shopId, string email, string content, int pageSize = -1, string continuationToke = null);
        Task<ResponseModel> SearchBookingsByRestaurant(int shopId, string email, string content, int pageSize = -1, string continuationToke = null);
        Task<ResponseModel> SearchBookingsByAdmin(int shopId, string content, int filterTime, DateTime stime, DateTime etime, int status, int pageSize = -1, string continuationToke = null);


        ResponseModel GetBookingItemAmount(List<BookingCourse> menuItems, PaymentTypeEnum paymentType, double payRate);
        Task<ResponseModel> GetBookingAmount(bool isBookingModify, string currency, string userId, double rate, List<string> Ids);
        void SettleOrder();
    }
    public class TrRestaurantBookingServiceHandler : ITrRestaurantBookingServiceHandler
    {
        private readonly IDbCommonRepository<TrDbRestaurant> _restaurantRepository;
        private readonly IDbCommonRepository<TrDbRestaurantBooking> _restaurantBookingRepository;
        private readonly IDbCommonRepository<StripeCheckoutSeesion> _stripeCheckoutSeesionRepository;
        private readonly IDbCommonRepository<DbCustomer> _customerRepository;
        private readonly IShopServiceHandler _shopServiceHandler;
        private readonly IContentBuilder _contentBuilder;
        ISendEmailUtil _sendEmailUtil;
        ILogManager _logger;
        ITwilioUtil _twilioUtil;
        IStripeUtil _stripeUtil;
        IHostingEnvironment _environment;
        private readonly IDbCommonRepository<DbShop> _shopRepository;
        private readonly ICustomerServiceHandler _customerServiceHandler;
        IMemoryCache _memoryCache;
        private readonly IDateTimeUtil _dateTimeUtil;
        IAmountCalculaterUtil _amountCalculaterV1;
        IPDFUtil _pDFUtil;
        private readonly AzureStorageConfig storageConfig;

        public TrRestaurantBookingServiceHandler(ITwilioUtil twilioUtil, IDbCommonRepository<TrDbRestaurant> restaurantRepository, IDbCommonRepository<TrDbRestaurantBooking> restaurantBookingRepository, IDbCommonRepository<DbCustomer> customerRepository,
            IDbCommonRepository<DbShop> shopRepository, ICustomerServiceHandler customerServiceHandler, IHostingEnvironment environment, IStripeUtil stripeUtil, IMemoryCache memoryCache, IShopServiceHandler shopServiceHandler,
            IDbCommonRepository<StripeCheckoutSeesion> stripeCheckoutSeesionRepository, IDateTimeUtil dateTimeUtil, IAmountCalculaterUtil amountCalculaterV1, ISendEmailUtil sendEmailUtil, Microsoft.Extensions.Options.IOptions<AzureStorageConfig> _storageConfig,
      IPDFUtil pDFUtil, ILogManager logger, IContentBuilder contentBuilder)
        {
            _restaurantRepository = restaurantRepository;
            _restaurantBookingRepository = restaurantBookingRepository;
            _stripeCheckoutSeesionRepository = stripeCheckoutSeesionRepository;
            _customerRepository = customerRepository;
            _contentBuilder = contentBuilder;
            _sendEmailUtil = sendEmailUtil;
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


        public async Task<TrDbRestaurantBooking> GetBooking(string id)
        {
            var Booking = await _restaurantBookingRepository.GetOneAsync(r => r.Id == id);
            return Booking;
        }

        public async Task<object> SettleBooking(string bookingId, string detailId, string userEmail)
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
            return new { code = 0, msg = "ok", };
        }
        public async Task<object> UpsetBookingRemark(string bookingId, string detailId, string remark, string userEmail)
        {
            var booking = await _restaurantBookingRepository.GetOneAsync(a => a.Id == bookingId);
            foreach (var item in booking.Details)
            {
                if (item.Id == detailId)
                {
                    item.Remark = remark;
                }
            }

            booking.Updater = userEmail;
            booking.Updated = DateTime.UtcNow;
            await _restaurantBookingRepository.UpsertAsync(booking);
            return new { code = 0, msg = "ok", };
        }
        public async Task<object> CancelBooking(string bookingId, string detailId, string userEmail, bool isAdmin)
        {//Europe/Dublin Europe/London Europe/Paris


            var booking = await _restaurantBookingRepository.GetOneAsync(a => a.Id == bookingId);
            foreach (var item in booking.Details)
            {
                if (item.Id == detailId)
                {
                    if (item.Status == OrderStatusEnum.Canceled)
                        return new { code = 0, msg = "订单已取消", };
                    else
                    {

                        //if (!isAdmin && (item.SelectDateTime.Value.GetLocaTimeByIANACode(_dateTimeUtil.GetIANACode(item.RestaurantCountry)) - DateTime.UtcNow.GetLocaTimeByIANACode(_dateTimeUtil.GetIANACode(item.RestaurantCountry))).TotalHours < 24)
                        //{
                        //return new { code = 0, msg = "距离用餐时间24小时内取消请联系客服人员：微信：groupmeals", };
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
                    _twilioUtil.sendSMS("+353874858555", $"你有订单: {booking.BookingRef}被取消。 请登录groupmeal.com查看更多");
                    var emailParams = EmailConfigs.Instance.Emails[EmailTypeEnum.MealCancelled];
                    _sendEmailUtil.SendCancelEmail(shopInfo, booking, item, _environment.WebRootPath, emailParams.TemplateName, emailParams.Subject, emailParams.CCEmail.ToArray());
                }
            }
            booking.Status = OrderStatusEnum.Canceled;
            booking.Updated = DateTime.UtcNow;
            booking.Updater = userEmail;
            OperationInfo operationInfo = new OperationInfo() { ModifyType = 3, Operater = userEmail, UpdateTime = DateTime.UtcNow, Operation = "订单取消" };
            booking.Operations.Add(operationInfo);

            var savedRestaurant = await _restaurantBookingRepository.UpsertAsync(booking);

            return new { code = 0, msg = "ok", };

        }
        public async Task<bool> UpdateBooking(string billId, string productId, string priceId)
        {
            TrDbRestaurantBooking booking = GetBooking(billId).Result;
            if (booking == null) return false;
            booking.PaymentInfos[0].StripeProductId = productId;
            booking.PaymentInfos[0].StripePriceId = priceId;
            var temp = await _restaurantBookingRepository.UpsertAsync(booking);
            return true;
        }
        private void UpdateStatus(BookingDetail item, int acceptType)
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
        public async Task<object> UpdateStatusByAdmin(string id, string dtlId, int status, DbToken user)
        {
            TrDbRestaurantBooking booking = await GetBooking(id);
            AcceptStatusEnum statusEnum = (AcceptStatusEnum)status;
            foreach (var item in booking.Details)
            {
                if (item.Id == dtlId)
                {
                    UpdateStatus(item, status);
                }
            }
            var opt = new OperationInfo() { Operater = user.UserId, Operation = statusEnum.ToString(), UpdateTime = DateTime.UtcNow };
            booking.Operations.Add(opt);
            var temp = await _restaurantBookingRepository.UpsertAsync(booking);
            return new ResponseModel { msg = "ok", code = 200, data = new { } };
        }
        public async Task<object> UpdateAccepted(string billId, string subBillId, int acceptType, string operater)
        {
            TrDbRestaurantBooking booking = GetBooking(billId).Result;
            if (booking.IsDeleted)
                return new { code = 1, msg = "Order Deleted(无效操作，订单已删除)", };
            foreach (var item in booking.Details)
            {
                if (item.Id == subBillId)
                {
                    switch (item.AcceptStatus)
                    {
                        case AcceptStatusEnum.UnAccepted:
                            UpdateStatus(item, acceptType);
                            break;
                        case AcceptStatusEnum.Accepted:
                            var customer = await _customerRepository.GetOneAsync(a => a.Email == operater);
                            if (customer != null)
                            {
                                bool IsAdmin = customer.AuthValue.AuthVerify(8);
                                if (IsAdmin)
                                {
                                    UpdateStatus(item, acceptType);
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
                                    UpdateStatus(item, acceptType);
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
                                    UpdateStatus(item, acceptType);
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

                }
            }
            if (booking == null) return false;
            //if (acceptType == 2)
            //{
            //    _stripeUtil.RefundGroupMeals(booking);
            //}
            var opt = new OperationInfo() { Operater = operater, Operation = acceptType == 1 ? "接收预订" : "拒绝预订", UpdateTime = DateTime.UtcNow };
            booking.Operations.Add(opt);
            var temp = await _restaurantBookingRepository.UpsertAsync(booking);
            string msg = $"您于{booking.BookingDate} {booking.BookingTime} 提交的订单已被接收，请按时就餐";
            if (acceptType == 2)
                msg = $"您于{booking.BookingDate} {booking.BookingTime} 提交的订单已被拒绝，请登录groupmeals.com查询别的餐厅";
            //_twilioUtil.sendSMS(booking.CustomerPhone, msg);
            var shopInfo = await _shopRepository.GetOneAsync(r => r.ShopId == booking.ShopId && r.IsActive.HasValue && r.IsActive.Value);
            if (shopInfo == null)
            {
                _logger.LogInfo("----------------Cannot find shop info" + booking.Id);
                return new { code = 500, msg = "Cannot find shop info", };
            }


            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    //System.Threading.Thread.Sleep(1000 * 60);
                    if (acceptType == 1)
                    {
                        //
                        var emailParams = EmailConfigs.Instance.Emails[EmailTypeEnum.MealAccepted];
                        BookingDetail detail = booking?.Details.FirstOrDefault(a => a.Id == subBillId);
                        _sendEmailUtil?.EmailCustomer(booking, detail, shopInfo, emailParams.TemplateName, _environment.WebRootPath, emailParams.Subject);

                        var emailParamsRest = EmailConfigs.Instance.Emails[EmailTypeEnum.MealAcceptedRestaurant];
                        var sendBooking = JsonConvert.DeserializeObject<TrDbRestaurantBooking>(JsonConvert.SerializeObject(booking));
                        sendBooking.Details.Clear();
                        sendBooking.Details.Add(detail);
                        _sendEmailUtil.EmailBoss(sendBooking, shopInfo, emailParamsRest.TemplateName, this._environment.WebRootPath, emailParamsRest.Subject);

                        //BackgroundJob.Schedule(() =>

                        //, TimeSpan.FromMinutes(1));
                    }
                    else if (acceptType == 2)
                    {
                        var emailParams = EmailConfigs.Instance.Emails[EmailTypeEnum.MealDeclined];
                        sendEmail(booking.Id, subBillId, shopInfo, emailParams.TemplateName, this._environment.WebRootPath, emailParams.Subject);
                        //  BackgroundJob.Schedule(() =>

                        //, TimeSpan.FromMinutes(1));
                    }
                }
                catch (Exception ex)
                {

                }
            });

            return new { code = 0, msg = "ok", data = booking };
        }

        public async void sendEmail(string bookingId, string subId, DbShop shopInfo, string tempName, string wwwPath, string subject)
        {
            try
            {
                var booking = await _restaurantBookingRepository.GetOneAsync(a => a.Id == bookingId);

                BookingDetail detail = booking?.Details.FirstOrDefault(a => a.Id == subId);

                _sendEmailUtil?.EmailCustomer(booking, detail, shopInfo, tempName, wwwPath, subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }



        }
        public async Task<bool> UpdateAcceptedReason(string billId, string subBillId, string reason, string operater)
        {
            TrDbRestaurantBooking booking = GetBooking(billId).Result;
            if (booking == null) return false;
            foreach (var item in booking.Details)
            {
                if (item.Id == subBillId)
                {
                    item.AcceptReason = reason;
                }
            }
            var opt = new OperationInfo() { Operater = operater, Operation = "添加原因", UpdateTime = DateTime.UtcNow };
            booking.Operations.Add(opt);
            var temp = await _restaurantBookingRepository.UpsertAsync(booking);
            return true;
        }

        public async Task<bool> BookingPaid(Stripe.Checkout.Session session)
        {
            try
            {
                _logger.LogInfo("BookingPaid.session");
                TrDbRestaurantBooking booking = await _restaurantBookingRepository.GetOneAsync(r => r.PaymentInfos[0].StripePriceId == session.Metadata["priceId"]);
                if (booking == null) return false;
                booking.PaymentInfos[0].Paid = true;

                _logger.LogInfo("BookingPaid.session.id:" + booking.Id);
                var temp = await _restaurantBookingRepository.UpsertAsync(booking);

                _logger.LogInfo("BookingPaid.UpdateAsync.session" + temp.Id);
                var newItem = await _stripeCheckoutSeesionRepository.UpsertAsync(new StripeCheckoutSeesion() { Data = session, BookingId = booking.Id });
            }
            catch (Exception ex)
            {
                _logger.LogInfo("BookingPaid.session.err" + ex);
            }
            return true;
        }
        public async Task<bool> BookingPaid(string bookingId, string customerId = "", string chargeId = "", string payMethodId = "", string receiptUrl = "")
        {
            try
            {


                _logger.LogInfo("----------------BookingPaid");
                TrDbRestaurantBooking booking = await _restaurantBookingRepository.GetOneAsync(r => r.Id == bookingId);
                if (booking == null)
                {
                    _logger.LogInfo("----------------bookingId: [" + bookingId + "] not found");
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(payMethodId))
                    booking.PaymentInfos[0].StripePaymentId = payMethodId;
                if (!string.IsNullOrWhiteSpace(customerId))
                {
                    booking.PaymentInfos[0].StripeCustomerId = customerId;
                    booking.PaymentInfos[0].StripeSetupIntent = true;
                }
                if (!string.IsNullOrWhiteSpace(receiptUrl))
                {

                    booking.PaymentInfos[0].StripeChargeId = chargeId;
                    booking.PaymentInfos[0].StripeReceiptUrl = receiptUrl;
                    booking.PaymentInfos[0].Paid = true;

                    booking.Status = Domain.Enum.OrderStatusEnum.UnAccepted;
                    booking.Details.ForEach(a => a.Status = OrderStatusEnum.UnAccepted);
                }
                _logger.LogInfo("----------------BookingPaid" + booking.Id);
                var temp = await _restaurantBookingRepository.UpsertAsync(booking);
                ClearCart(temp);
                var shopInfo = await _shopRepository.GetOneAsync(r => r.ShopId == booking.ShopId && r.IsActive.HasValue && r.IsActive.Value);
                if (shopInfo == null)
                {
                    _logger.LogInfo("----------------Cannot find shop info" + booking.Id);
                    throw new ServiceException("Cannot find shop info");
                }


                var emailParams = EmailConfigs.Instance.Emails[EmailTypeEnum.NewMealCustomer];
                _sendEmailUtil.EmailCustomerTotal(booking, shopInfo, emailParams.TemplateName, this._environment.WebRootPath, emailParams.Subject);
                emailParams = EmailConfigs.Instance.Emails[EmailTypeEnum.NewMealRestaurant];
                _sendEmailUtil.EmailBoss(booking, shopInfo, emailParams.TemplateName, this._environment.WebRootPath, emailParams.Subject);
                //var newItem = await _stripeCheckoutSeesionRepository.CreateAsync(new StripeCheckoutSeesion() { Data = session, BookingId = booking.Id });
            }
            catch (Exception ex)
            {
                _logger.LogInfo("----------------BookingPaid.err" + ex.Message + "： " + ex.StackTrace);
            }
            return true;
        }
        private async void ClearCart(TrDbRestaurantBooking booking)
        {

            var customers = await _customerServiceHandler.List((int)booking.ShopId);
            var customer = customers.FirstOrDefault(a => a.Email == booking.CustomerEmail);
            if ((customer != null))
            {
                customer.CartInfos.Clear();
                _customerServiceHandler.UpdateCart(new List<BookingDetail>(), customer.Id, (int)booking.ShopId);
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
        public async Task<ResponseModel> ModifyBooking(TrDbRestaurantBooking booking, int shopId, string email, bool isNotify = true)
        {
            Guard.NotNull(booking);
            Guard.AreEqual(booking.ShopId.Value, shopId);
            var exsitBooking = await _restaurantBookingRepository.GetOneAsync(r => !r.IsDeleted && r.Id == booking.Id);
            if (exsitBooking == null) return new ResponseModel { msg = "booking not found", code = 200, data = null };
            OperationInfo operationInfo = new OperationInfo() { ModifyType = 4, Operater = email, UpdateTime = DateTime.UtcNow, Operation = "订单修改" };
            int isChange = 0; int isDtlChanged = 0;
            foreach (var item in exsitBooking.Details)
            {

                var detail = booking.Details.FirstOrDefault(d => d.Id == item.Id);
                if (detail != null)
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

                    var res = UpdateField(operationInfo, booking.Id, item, detail, "SelectDateTime");
                    if (res) isChange++;
                    res = UpdateField(operationInfo, booking.Id, item, detail, "Memo");
                    if (res) isChange++;
                    res = UpdateField(operationInfo, booking.Id, item, detail, "GroupRef");
                    if (res) isChange++;
                    res = UpdateField(operationInfo, booking.Id, item, detail, "ContactName");
                    if (res) isChange++;
                    res = UpdateField(operationInfo, booking.Id, item, detail, "ContactPhone");
                    if (res) isChange++;
                    res = UpdateField(operationInfo, booking.Id, item, detail, "ContactWechat");
                    if (res) isChange++;
                    res = UpdateField(operationInfo, booking.Id, item, detail, "ContactInfos");
                    if (res) isChange++;
                    res = UpdateField(operationInfo, booking.Id, item, detail, "RestaurantId");
                    if (res) isChange++;
                    res = UpdateField(operationInfo, booking.Id, item, detail, "RestaurantName", false);
                    if (res) isChange++;
                    res = UpdateField(operationInfo, booking.Id, item, detail, "RestaurantEmail", false);
                    if (res) isChange++;
                    res = UpdateField(operationInfo, booking.Id, item, detail, "RestaurantAddress", false);
                    if (res) isChange++;
                    res = UpdateField(operationInfo, booking.Id, item, detail, "RestaurantPhone", false);
                    if (res) isChange++;
                    res = UpdateField(operationInfo, booking.Id, item, detail, "EmergencyPhone", false);
                    if (res) isChange++;
                    res = UpdateField(operationInfo, booking.Id, item, detail, "RestaurantWechat", false);
                    if (res) isChange++;


                    var Oldamount = _amountCalculaterV1.getItemAmount(item);
                    var amount = _amountCalculaterV1.getItemAmount(detail);
                    if (amount != Oldamount)
                    {
                        isChange++;
                        UpdateListField(operationInfo, booking.Id, item, detail, "Courses");
                        item.Courses = detail.Courses;

                        AmountInfo amountInfo = new AmountInfo() { Id = "A" + SnowflakeId.getSnowId() };
                        amountInfo.Amount = amount - Oldamount;//新增差价记录
                        item.AmountInfos.Add(amountInfo);
                    }
                }
                if (isDtlChanged == isChange)
                    item.Modified = false;
                else
                    item.Modified = true;
                isDtlChanged = isChange;
            }
            if (isChange > 0)
            {
                var temo = _amountCalculaterV1.CalculateOrderPaidAmount(exsitBooking, 0.8);
                exsitBooking.Operations.Add(operationInfo);
                var savedBooking = await _restaurantBookingRepository.UpsertAsync(exsitBooking);
                TrDbRestaurantBooking sendBooking = JsonConvert.DeserializeObject<TrDbRestaurantBooking>(JsonConvert.SerializeObject(savedBooking));
                sendBooking.Details.Clear();
                foreach (var item in exsitBooking.Details)
                {
                    if (item.Modified)
                        sendBooking.Details.Add(item);
                }
                if (isNotify)
                    SendModifyEmail(sendBooking);
            }
            return new ResponseModel { msg = "", code = 200, data = null };
        }

        private bool UpdateField(OperationInfo operationInfo, string bookingId, BookingDetail item, BookingDetail detail, string fieldName, bool record = true)
        {
            bool isChange = false;
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
                    modifyInfo.ModifyLocation = $"{bookingId}>{item.Id}";
                    modifyInfo.oldValue = oldValue?.ToString();
                    modifyInfo.newValue = newValue?.ToString();
                    operationInfo.ModifyInfos.Add(modifyInfo);
                }
                item.GetType().GetProperty(fieldName).SetValue(item, newValue);
            }
            return isChange;
        }
        private bool UpdateListField(OperationInfo operationInfo, string bookingId, BookingDetail item, BookingDetail detail, string fieldName)
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
                modifyInfo.ModifyLocation = $"{bookingId}>{item.Id}";
                modifyInfo.oldValue = oldValue?.ToString();
                modifyInfo.newValue = newValue?.ToString();
                operationInfo.ModifyInfos.Add(modifyInfo);
            }
            return isChange;
        }


        public async Task<ResponseModel> MakeABooking(TrDbRestaurantBooking booking, int shopId, DbToken user)
        {
            _logger.LogInfo("RequestBooking" + user.UserEmail);
            Guard.NotNull(booking);
            Guard.AreEqual(booking.ShopId.Value, shopId);
            //foreach (var item in booking.Details)
            //{
            //    _logger.LogInfo(" Make a booking.time: " + item.SelectDateTime);
            //    if ((item.SelectDateTime - DateTime.UtcNow).Value.TotalHours < 12)
            //    {
            //        return new ResponseModel { msg = "用餐时间少于12个小时", code = 200, data = null };
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
            bool noPay = await InitBooking(booking, user.UserId);


            var shopInfo = await _shopRepository.GetOneAsync(r => r.ShopId == booking.ShopId && r.IsActive.HasValue && r.IsActive.Value);
            if (shopInfo == null)
            {
                _logger.LogInfo("----------------Cannot find shop info" + booking.Id);
                return new ResponseModel { msg = "Cannot find shop info", code = 500, };
            }

            double exchange = (double)shopInfo.ExchangeRate;
            booking.PaymentInfos.Add(new PaymentInfo() { Amount = _amountCalculaterV1.CalculateOrderAmount(booking, exchange), PaidAmount = _amountCalculaterV1.CalculateOrderPaidAmount(booking, exchange) });

            var newItem = await _restaurantBookingRepository.UpsertAsync(booking);
            if (noPay)
            {
                ClearCart(newItem);
                SendEmail(newItem);
            }
            return new ResponseModel { msg = "", code = 200, data = newItem };
        }
        private async Task<bool> InitBooking(TrDbRestaurantBooking booking, string userId)
        {
            if (string.IsNullOrWhiteSpace(booking.CustomerEmail))
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
                if (string.IsNullOrWhiteSpace(item.BookingRef))
                {
                    item.BookingRef = "GM" + SnowflakeId.getSnowId();
                }
                if (string.IsNullOrWhiteSpace(item.RestaurantEmail))
                {
                    var rest = await _restaurantRepository.GetOneAsync(a => a.Id == item.RestaurantId);
                    item.RestaurantName = rest.StoreName;
                    item.RestaurantEmail = rest.Email;
                    item.RestaurantAddress = rest.Address;
                    item.RestaurantPhone = rest.PhoneNumber;
                    item.EmergencyPhone = rest.ContactPhone;
                    item.RestaurantWechat = rest.Wechat;
                }


                if (string.IsNullOrWhiteSpace(item.Id))
                    item.Id = Guid.NewGuid().ToString();

                if (item.ContactEmail == null)
                {
                    var user = await _customerRepository.GetOneAsync(a => a.Id == userId);
                    item.ContactEmail = user.Email;
                    item.ContactName = user.UserName;
                    item.ContactPhone = user.Phone;
                    item.ContactWechat = user.WeChat;
                }

                //foreach (var course in item.Courses)
                //{
                //    if (string.IsNullOrWhiteSpace(course.Id))
                //        course.Id = "C" + SnowflakeId.getSnowId();
                //}

                if (item.AmountInfos.Count() == 0)
                {
                    AmountInfo amountInfo = new AmountInfo()
                    {
                        Id = "A" + SnowflakeId.getSnowId(),
                        Amount = _amountCalculaterV1.getItemAmount(item),
                        PaidAmount = _amountCalculaterV1.getItemPayAmount(item)
                    };
                    item.AmountInfos.Add(amountInfo);
                }

                foreach (var amount in item.AmountInfos)
                {
                    if (string.IsNullOrWhiteSpace(amount.Id))
                        amount.Id = "A" + SnowflakeId.getSnowId();
                }

                if (item.BillInfo.PaymentType != PaymentTypeEnum.PayAtStore)
                    noPay = false;

                if (noPay)
                    item.Status = OrderStatusEnum.UnAccepted;
            }
            if (noPay)
            {
                booking.Status = OrderStatusEnum.UnAccepted;

            }
            return noPay;
        }
        private async void SendEmail(TrDbRestaurantBooking booking)
        {
            var shopInfo = await _shopRepository.GetOneAsync(r => r.ShopId == booking.ShopId && r.IsActive.HasValue && r.IsActive.Value);
            if (shopInfo == null)
            {
                _logger.LogInfo("----------------Cannot find shop info" + booking.Id);
                throw new ServiceException("Cannot find shop info");
            }
            _twilioUtil.sendSMS("+353874858555", $"你有新的订单: {booking.BookingRef}。 请登录groupmeal.com查看更多");

            var emailParams = EmailConfigs.Instance.Emails[EmailTypeEnum.NewMealCustomer];
            _sendEmailUtil.EmailCustomerTotal(booking, shopInfo, emailParams.TemplateName, this._environment.WebRootPath, emailParams.Subject);
            emailParams = EmailConfigs.Instance.Emails[EmailTypeEnum.NewMealRestaurant];
            _sendEmailUtil.EmailBoss(booking, shopInfo, emailParams.TemplateName, this._environment.WebRootPath, emailParams.Subject);
            //EmailUtils.EmailSupport(booking, shopInfo, "new_meals_support", this._environment.WebRootPath, "New Booking", _twilioUtil, _contentBuilder,  _logger);

        }
        private async void SendModifyEmail(TrDbRestaurantBooking booking)
        {
            var shopInfo = await _shopRepository.GetOneAsync(r => r.ShopId == booking.ShopId && r.IsActive.HasValue && r.IsActive.Value);
            if (shopInfo == null)
            {
                _logger.LogInfo("----------------Cannot find shop info" + booking.Id);
                throw new ServiceException("Cannot find shop info");
            }
            _twilioUtil.sendSMS(booking.CustomerPhone, $"你有订单被修改: {booking.BookingRef}。 请登录groupmeal.com查看更多");
            var emailParams = EmailConfigs.Instance.Emails[EmailTypeEnum.MealModified];
            _sendEmailUtil.EmailCustomerTotal(booking, shopInfo, emailParams.TemplateName, this._environment.WebRootPath, emailParams.Subject);

            //if (booking.Details[0].Status == OrderStatusEnum.UnAccepted)//订单未接收时重发接收邮件
            //{
            //    EmailUtils.EmailBoss(booking, shopInfo, emailParams.TemplateName, this._environment.WebRootPath, emailParams.Subject);
            //}
            //else

            _sendEmailUtil.EmailBoss(booking, shopInfo, emailParams.TemplateName, this._environment.WebRootPath, emailParams.Subject);

        }
        public async Task<bool> DoRebate(string bookingId, double rebate)
        {

            TrDbRestaurantBooking booking = await _restaurantBookingRepository.GetOneAsync(r => r.Id == bookingId);
            if (booking != null)
            {
                booking.Rebate = rebate;
                await _restaurantBookingRepository.UpsertAsync(booking);
            }

            return true;
        }

        public async Task<bool> ResendEmail(string bookingId)
        {
            _logger.LogInfo("ResendEmail.bookingid:" + bookingId);

            try
            {
                TrDbRestaurantBooking booking = await _restaurantBookingRepository.GetOneAsync(r => r.Id == bookingId);
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
                    _sendEmailUtil.EmailBoss(booking, shopInfo, emailParams.TemplateName, this._environment.WebRootPath, emailParams.Subject);
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
        public async Task<bool> UpdateStripeClientKey(string bookingId, string paymentId, string customerId, string secertKey)
        {
            TrDbRestaurantBooking booking = await _restaurantBookingRepository.GetOneAsync(r => r.Id == bookingId);
            if (booking == null)
            {
                _logger.LogInfo("bookingId: [" + bookingId + "] not found");
                return false;
            }

            if (!string.IsNullOrWhiteSpace(secertKey))
            {
                booking.PaymentInfos[0].StripeCustomerId = customerId;
                booking.PaymentInfos[0].StripeClientSecretKey = secertKey;
                booking.PaymentInfos[0].StripeSetupIntent = true;
                booking.PaymentInfos[0].StripePaymentId = paymentId;
            }
            var temp = await _restaurantBookingRepository.UpsertAsync(booking);

            //var newItem = await _stripeCheckoutSeesionRepository.CreateAsync(new StripeCheckoutSeesion() { Data = session, BookingId = booking.Id });

            return true;
        }
        public async Task<TrDbRestaurantBooking> BindingPayInfoToTourBooking(TrDbRestaurantBooking booking, string PaymentId, string stripeClientSecretKey, bool isSetupPay)
        {
            Guard.NotNull(booking);
            booking.PaymentInfos[0].StripePaymentId = PaymentId;
            booking.PaymentInfos[0].SetupPay = isSetupPay;
            booking.PaymentInfos[0].StripeClientSecretKey = stripeClientSecretKey;
            var res = await _restaurantBookingRepository.UpsertAsync(booking);
            return res;
        }
        public async Task<bool> DeleteBooking(string bookingId, int shopId)
        {
            var booking = await _restaurantBookingRepository.GetOneAsync(a => a.Id == bookingId);
            booking.IsDeleted = true;
            booking.Updated = DateTime.UtcNow;
            var savedRestaurant = await _restaurantBookingRepository.UpsertAsync(booking);
            return savedRestaurant != null;
        }

        public async Task<bool> DeleteBookingDetail(string bookingId, string detailId, int shopId)
        {
            var booking = await _restaurantBookingRepository.GetOneAsync(a => a.Id == bookingId);
            bool isAllDeleted = true;
            foreach (var bookingDetail in booking.Details)
            {
                if (bookingDetail.Id == detailId)
                {
                    bookingDetail.IsDeleted = true;
                }
                if (!bookingDetail.IsDeleted)
                    isAllDeleted = false;
            }
            if (isAllDeleted)
                booking.IsDeleted = true;
            booking.Updated = DateTime.UtcNow;
            var savedRestaurant = await _restaurantBookingRepository.UpsertAsync(booking);
            return savedRestaurant != null;
        }
        public async Task<bool> UndoDeleteDetail(string bookingId, string detailId, int shopId)
        {
            var booking = await _restaurantBookingRepository.GetOneAsync(a => a.Id == bookingId);
            foreach (var bookingDetail in booking.Details)
            {
                if (bookingDetail.Id == detailId)
                {
                    bookingDetail.IsDeleted = false;
                    booking.IsDeleted = false;
                }
            }
            booking.Updated = DateTime.UtcNow;
            var savedRestaurant = await _restaurantBookingRepository.UpsertAsync(booking);
            return savedRestaurant != null;
        }
        public async Task<ResponseModel> SearchBookings(int shopId, string email, string content, int pageSize = -1, string continuationToken = null)
        {
            pageSize = -1;
            //SettleOrder();
            List<TrDbRestaurantBooking> res = new List<TrDbRestaurantBooking>();
            string pageToken = "";
            if (string.IsNullOrWhiteSpace(content))
            {
                var Bookings = await _restaurantBookingRepository.GetManyAsync(a => (a.Status != OrderStatusEnum.None && !a.IsDeleted && a.CustomerEmail == email), pageSize, continuationToken);
                res = Bookings.Value.ToList();
                pageToken = Bookings.Key;


            }
            else
            {
                var Bookings = await _restaurantBookingRepository.GetManyAsync(a => (a.Status != OrderStatusEnum.None && !a.IsDeleted && a.CustomerEmail == email && a.Details.Any(d => d.RestaurantName.ToLower().Contains(content.ToLower()))), pageSize, continuationToken);
                res = Bookings.Value.ToList();
                pageToken = Bookings.Key;

            }
            res.ForEach(r => { r.Details.OrderByDescending(d => d.SelectDateTime); });
            var list = res.OrderByDescending(a => a.Details.Max(d => d.SelectDateTime)).ToList();

            return new ResponseModel { msg = "ok", code = 200, token = pageToken, data = list };

        }
        public async void SettleOrder()
        {
            try
            {
                DateTime stime = DateTime.UtcNow;
                var Bookings = await _restaurantBookingRepository.GetManyAsync(a => (a.Status != OrderStatusEnum.None && !a.IsDeleted));
                var span = (DateTime.UtcNow - stime).TotalMilliseconds;
                Console.WriteLine(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff") + " : " + span);
                var list = Bookings.ToList();
                foreach (var item in list)
                {
                    bool isSettled = true;
                    foreach (var b in item.Details)
                    {
                        if (b.AcceptStatus == AcceptStatusEnum.Accepted && b.SelectDateTime < DateTime.UtcNow)
                        {

                        }
                        else
                        {
                            isSettled = false;
                        }
                    }
                    if (isSettled && item.Status != OrderStatusEnum.Settled)
                    {
                        item.Status = OrderStatusEnum.Settled;
                        item.Details.ForEach(a => { a.Status = OrderStatusEnum.Settled; a.AcceptStatus = AcceptStatusEnum.Settled; });
                        await _restaurantBookingRepository.UpsertAsync(item);
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
            List<TrDbRestaurantBooking> list = new List<TrDbRestaurantBooking>();
            string token = "";

            if (string.IsNullOrWhiteSpace(content))
            {
                var Bookings = await _restaurantBookingRepository.GetManyAsync(a => (a.Status != OrderStatusEnum.None && !a.IsDeleted) || a.Details.Any(b => b.RestaurantEmail == email), pageSize, continuationToken);
                list = Bookings.Value.ToList();
                token = Bookings.Key;
            }

            else
            {
                var Bookings = await _restaurantBookingRepository.GetManyAsync(a => (a.Status != OrderStatusEnum.None && !a.IsDeleted) || a.Details.Any(b => b.RestaurantEmail == email), pageSize, continuationToken);
                list = Bookings.Value.ToList().FindAll(a => a.Details.Any(d => d.RestaurantName.ToLower().Contains(content.ToLower()))).ToList();
                token = Bookings.Key;
            }


            var res = JsonConvert.DeserializeObject<List<TrDbRestaurantBooking>>(JsonConvert.SerializeObject(list));
            foreach (var item in res)
            {
                item.Details.Clear();
                foreach (var rest in list)
                {
                    item.Details.AddRange(rest.Details.FindAll(a => rest.Id == item.Id && a.RestaurantEmail == email));
                }
            }

            return new ResponseModel { msg = "ok", code = 200, token = token, data = res };
        }

        public async Task<ResponseModel> SearchBookingsByAdmin(int shopId, string content, int filterTime, DateTime stime, DateTime etime, int status, int pageSize = -1, string continuationToken = null)
        {
            List<TrDbRestaurantBooking> res = new List<TrDbRestaurantBooking>();
            string pageToken = "";
            KeyValuePair<string, IEnumerable<TrDbRestaurantBooking>> Bookings = new KeyValuePair<string, IEnumerable<TrDbRestaurantBooking>>();
            bool emptyTime = stime == DateTime.MinValue || etime == DateTime.MinValue;
            if (string.IsNullOrWhiteSpace(content))
            {
                if (status == 6)
                {
                    if (emptyTime)
                    {
                        Bookings = await _restaurantBookingRepository.GetManyAsync(a => (a.Status != OrderStatusEnum.None && a.IsDeleted), pageSize, continuationToken);
                        res = Bookings.Value.ToList();
                    }
                    else
                    {
                        etime = etime.AddDays(1);
                        if (filterTime == 1)
                        {
                            Bookings = await _restaurantBookingRepository.GetManyAsync(a => (a.Status != OrderStatusEnum.None && a.IsDeleted &&
                            a.Details.Any(d => d.SelectDateTime > stime && d.SelectDateTime <= etime)), pageSize, continuationToken);
                            res = Bookings.Value.ToList();
                            foreach (var order in res)
                            {
                                order.Details = order.Details.FindAll(d => d.SelectDateTime > stime && d.SelectDateTime <= etime);
                            }
                        }
                        else
                        {
                            Bookings = await _restaurantBookingRepository.GetManyAsync(a => (a.Status != OrderStatusEnum.None && a.IsDeleted &&
                            a.Created > stime && a.Created <= etime), pageSize, continuationToken);
                            res = Bookings.Value.ToList();
                        }

                    }

                }
                else if (status == -1)
                {
                    if (emptyTime)
                    {
                        Bookings = await _restaurantBookingRepository.GetManyAsync(a => (a.Status != OrderStatusEnum.None && !a.IsDeleted), pageSize, continuationToken);
                        res = Bookings.Value.ToList();
                    }
                    else
                    {
                        etime = etime.AddDays(1);
                        if (filterTime == 1)
                        {
                            Bookings = await _restaurantBookingRepository.GetManyAsync(a => (a.Status != OrderStatusEnum.None && !a.IsDeleted &&
                            a.Details.Any(d => d.SelectDateTime > stime && d.SelectDateTime <= etime)), pageSize, continuationToken);
                            res = Bookings.Value.ToList();
                            foreach (var order in res)
                            {
                                order.Details = order.Details.FindAll(d => d.SelectDateTime > stime && d.SelectDateTime <= etime);
                            }
                        }
                        else
                        {
                            Bookings = await _restaurantBookingRepository.GetManyAsync(a => (a.Status != OrderStatusEnum.None && !a.IsDeleted &&
                            a.Created > stime && a.Created <= etime), pageSize, continuationToken);
                            res = Bookings.Value.ToList();
                        }
                    }
                }
                else
                {
                    if (emptyTime)
                    {
                        Bookings = await _restaurantBookingRepository.GetManyAsync(a => (a.Status != OrderStatusEnum.None && !a.IsDeleted &&
                        a.Details.Any(d => d.Status == (OrderStatusEnum)status)), pageSize, continuationToken);
                        res = Bookings.Value.ToList();
                        foreach (var order in res)
                        {
                            order.Details = order.Details.FindAll(d => d.Status == (OrderStatusEnum)status);
                        }
                    }
                    else
                    {
                        etime = etime.AddDays(1);
                        if (filterTime == 1)
                        {
                            Bookings = await _restaurantBookingRepository.GetManyAsync(a => (a.Status != OrderStatusEnum.None && !a.IsDeleted &&
                            a.Details.Any(d => d.Status == (OrderStatusEnum)status && d.SelectDateTime > stime && d.SelectDateTime <= etime)), pageSize, continuationToken);
                            res = Bookings.Value.ToList();
                            foreach (var order in res)
                            {
                                order.Details = order.Details.FindAll(d => d.Status == (OrderStatusEnum)status && d.SelectDateTime > stime && d.SelectDateTime <= etime);
                            }
                        }
                        else
                        {
                            Bookings = await _restaurantBookingRepository.GetManyAsync(a => (a.Status != OrderStatusEnum.None && !a.IsDeleted &&
                            a.Created > stime && a.Created <= etime && a.Details.Any(d => d.Status == (OrderStatusEnum)status)), pageSize, continuationToken);
                            res = Bookings.Value.ToList();
                            foreach (var order in res)
                            {
                                order.Details = order.Details.FindAll(d => d.Status == (OrderStatusEnum)status);
                            }
                        }
                    }

                }


            }
            else
            {
                content = content.ToLower().Trim();
                Predicate<BookingDetail> predicate =
                    d => d.RestaurantName.ToLower().Contains(content) || d.RestaurantAddress.ToLower().Contains(content) || d.ContactName.ToLower().Contains(content) || d.GroupRef.ToLower().Contains(content);

                Bookings = await _restaurantBookingRepository.GetManyAsync(a => ((a.Status != OrderStatusEnum.None) &&
                (a.BookingRef.ToLower().Contains(content) || a.Details.Any(d => d.RestaurantName.ToLower().Contains(content) || d.RestaurantAddress.ToLower().Contains(content) ||
                d.ContactName.ToLower().Contains(content) || d.GroupRef.ToLower().Contains(content)
                    ))), pageSize, continuationToken);

                res = Bookings.Value.ToList();
                foreach (var order in res)
                {
                    if (!order.BookingRef.ToLower().Contains(content))//主单未命才筛选子单
                        order.Details = order.Details.FindAll(predicate);
                }


            }


            pageToken = Bookings.Key;
            //res.ForEach(r => { r.Details.OrderByDescending(d => d.SelectDateTime); });
            //var list = res.OrderByDescending(a => a.Created).ToList();

            return new ResponseModel { msg = "ok", code = 200, token = pageToken, data = res };
        }

        public ResponseModel GetBookingItemAmount(List<BookingCourse> menuItems, PaymentTypeEnum paymentType, double payRate)
        {
            if (paymentType == PaymentTypeEnum.Deposit && payRate <= 0)
                return new ResponseModel { msg = "payRate should greater than 0", code = 500 };
            decimal amount = 0, paidAmount = 0;
            BookingDetail detail = new BookingDetail() { Courses = menuItems, BillInfo = new RestaurantBillInfo() { PaymentType = paymentType, PayRate = payRate } };
            paidAmount = _amountCalculaterV1.getItemPayAmount(detail);
            amount = _amountCalculaterV1.getItemAmount(detail);
            return new ResponseModel { msg = "ok", code = 200, data = new { amount, paidAmount } };

        }
        public async Task<ResponseModel> GetBookingAmount(bool isBookingModify, string currency, string userId, double rate, List<string> Ids)
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
                    return new ResponseModel { msg = "detailId can't find in Order list", code = 500, };

                }

                foreach (var item in Bookings)
                {
                    var items = item.Details.FindAll(d => Ids.Contains(d.Id));
                    foreach (var detail in items)
                    {
                        totalPayAmount += detail.AmountInfos.Sum(a => a.PaidAmount);
                        if (detail.Currency == "UK")
                        {
                            UKAmount += _amountCalculaterV1.getItemAmount(detail);
                            UKPaidAmount += detail.AmountInfos.Sum(a => a.PaidAmount);


                        }
                        else
                        {
                            EUAmount += _amountCalculaterV1.getItemAmount(detail);
                            EUPaidAmount += detail.AmountInfos.Sum(a => a.PaidAmount);
                        }
                    }
                }
            }
            else
            {
                DateTime stime = DateTime.UtcNow;
                var user = await _customerRepository.GetOneAsync(a => a.Id == userId);
                Console.WriteLine("cart:" + (DateTime.UtcNow - stime).TotalMilliseconds);
                var details = user.CartInfos.FindAll(c => Ids.Contains(c.Id));
                if (details == null || details.Count() == 0)
                {
                    return new ResponseModel { msg = "detailId can't find in cartinfo", code = 500, };

                }
                foreach (var detail in details)
                {
                    if (currency == detail.Currency)
                    {
                        totalPayAmount += _amountCalculaterV1.getItemPayAmount(detail);
                    }
                    else
                    {
                        if (detail.Currency == "UK")
                        {
                            totalPayAmount += _amountCalculaterV1.getItemPayAmount(detail) / (decimal)rate;
                        }
                        else
                            totalPayAmount += _amountCalculaterV1.getItemPayAmount(detail) * (decimal)rate;
                    }
                    if (detail.Currency == "UK")
                    {
                        UKAmount += _amountCalculaterV1.getItemAmount(detail);
                        UKPaidAmount += _amountCalculaterV1.getItemPayAmount(detail);
                    }
                    else
                    {
                        EUAmount += _amountCalculaterV1.getItemAmount(detail);
                        EUPaidAmount += _amountCalculaterV1.getItemPayAmount(detail);
                    }
                }

            }
            Console.WriteLine("Total: " + (DateTime.UtcNow - sdate).TotalMilliseconds);
            return new ResponseModel { msg = "ok", code = 200, data = new { EUAmount, UKAmount, EUPaidAmount, UKPaidAmount, totalPayAmount } };

        }
        public async Task<ResponseModel> GetSchedulePdf(DbToken userId)
        {
            List<PDFModel> pdfData = new List<PDFModel>();
            var Bookings = await _restaurantBookingRepository.GetManyAsync(a => (a.Status != OrderStatusEnum.None && !a.IsDeleted &&
        a.Details.Any(d => !d.IsDeleted && d.Status != OrderStatusEnum.Canceled && d.Status != OrderStatusEnum.Settled) && a.CustomerEmail == userId.UserEmail));
            foreach (var Booking in Bookings)
            {
                foreach (var detail in Booking.Details)
                {
                    if (detail.IsDeleted || detail.Status == OrderStatusEnum.Canceled || detail.Status == OrderStatusEnum.Settled) continue;
                    Console.WriteLine(Booking.BookingRef + "." + detail.Status.ToString() + " : " + detail.AcceptStatus.ToString());
                    if ((int)detail.AcceptStatus > 1) continue;//只加入待接单与已接单的
                    string selectDateTimeStr = detail.SelectDateTime.Value.GetLocaTimeByIANACode(_dateTimeUtil.GetIANACode(detail.RestaurantCountry ?? "UK")).ToString("yyyy-MM-dd HH:mm:ss");
                    string mealStr = "";
                    foreach (var meal in detail.Courses)
                    {
                        mealStr += $"{meal.MenuItemName}({meal.Price})*{meal.Qty + meal.ChildrenQty}{Environment.NewLine}";
                    }
                    decimal amount = detail.AmountInfos.Sum(a => a.Amount);
                    string currencyStr = detail.Currency == "" ? "" : "";
                    string mealInfo = $"{mealStr}{currencyStr} ";
                    var model = new PDFModel
                    {
                        mealInfo = mealInfo,
                        BookingRef = Booking.BookingRef,
                        BookingTime = selectDateTimeStr,
                        RestuarantName = detail.RestaurantName,
                        Phone = detail.RestaurantPhone,
                        ContactPhone = detail.EmergencyPhone,
                        Email = detail.ContactEmail,
                        Wechat = detail.ContactWechat,
                        Remark = detail.Memo,
                        Address = detail.RestaurantAddress
                    };
                    pdfData.Add(model);
                }
            }

            var doc = _pDFUtil.GeneratePdf(pdfData);
            Stream fileStream = new MemoryStream(doc.GeneratePdf());
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
            var Bookings = await _restaurantBookingRepository.GetManyAsync(a => (a.Status != OrderStatusEnum.None));
            int totalQty = Bookings.Sum(a => a.Details.Count);
            var monthBooking = Bookings.Where(a => a.Created > DateTime.UtcNow.AddDays(1 - DateTime.UtcNow.Day).Date).ToList();
            int monthQty = monthBooking.Sum(a => a.Details.Count);
            var weekBooking = Bookings.Where(a => a.Created > DateTime.UtcNow.Date.AddDays(-7)).ToList();
            int todayQty = weekBooking.Where(a => a.Created > DateTime.UtcNow.Date).ToList().Sum(a => a.Details.Count);
            int week = weekBooking.Sum(a => a.Details.Count);
            List<BookingReport> weekly = new List<BookingReport>();

            weekly.Add(new BookingReport() { Name = DateTime.UtcNow.ToString("yyyy-MM-dd") + " (" + todayQty + ")", Qty = todayQty });
            int weekQty = 0;
            for (int i = 1; i < 7; i++)
            {
                weekQty = weekly.Sum(a => a.Qty);
                int dayqty = weekBooking.Where(a => a.Created > DateTime.UtcNow.Date.AddDays(-i)).ToList().Sum(a => a.Details.Count) - weekQty;
                weekly.Add(new BookingReport() { Name = DateTime.UtcNow.Date.AddDays(-i).ToString("yyyy-MM-dd") + " (" + dayqty + ")", Qty = dayqty });
            }
            weekly.Reverse();
            return new ResponseModel { msg = "ok", code = 200, data = new { totalQty, todayQty, weekQty, monthQty, weekly } };
        }
        public async Task<bool> OrderCheck()
        {
           DateTime time= DateTime.UtcNow.GetLocaTimeByIANACode(_dateTimeUtil.GetIANACode("Ireland"));
            if(time.Hour<18&&time.Hour>8) { 
            var bookings = await _restaurantBookingRepository.GetManyAsync(a => (a.Status != OrderStatusEnum.UnAccepted && (a.Created.Value - DateTime.UtcNow).TotalMinutes > 30));
                if (bookings != null && bookings.Count() > 0)
                {
                    _logger.LogInfo($"你有[{bookings.Count()}]张订单超过30分钟未接单，请登录groupmeal.com查看更多");
                    _twilioUtil.sendSMS("+353874858555", $"你有[{bookings.Count()}]张订单超过30分钟未接单，请登录groupmeal.com查看更多"); 
                }
            }
            return true;
        }

    }
}
