using App.Domain.TravelMeals.Restaurant;
using App.Domain.TravelMeals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using App.Domain.Common.Shop;
using App.Infrastructure.Builders.Common;
using App.Infrastructure.Builders.TravelMeals;
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
using System.Drawing.Printing;

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
        Task<object> UpdateAccepted(string bookingId, string subBillId, int acceptType, string operater);
        Task<bool> UpdateAcceptedReason(string bookingId, string subBillId, string reason, string operater);
        Task<bool> CancelBooking(string bookingId, string detailId, int shopId, string userEmail);
        Task<ResponseModel> RequestBooking(TrDbRestaurantBooking booking, int shopId, string userId);
        Task<ResponseModel> ModifyBooking(TrDbRestaurantBooking booking, int shopId, string email);
        Task<bool> DeleteBooking(string bookingId, int shopId);
        Task<ResponseModel> SearchBookings(int shopId, string email, string content, int pageSize = -1, string continuationToke = null);
        Task<ResponseModel> SearchBookingsByRestaurant(int shopId, string email, string content, int pageSize = -1, string continuationToke = null);
        void SettleOrder();
    }
    public class TrRestaurantBookingServiceHandler : ITrRestaurantBookingServiceHandler
    {
        private readonly IDbCommonRepository<TrDbRestaurant> _restaurantRepository;
        private readonly IDbCommonRepository<TrDbRestaurantBooking> _restaurantBookingRepository;
        private readonly IDbCommonRepository<StripeCheckoutSeesion> _stripeCheckoutSeesionRepository;
        private readonly IDbCommonRepository<DbCustomer> _customerRepository;
        private readonly IContentBuilder _contentBuilder;
        private readonly IEmailUtil _emailUtil;
        ILogManager _logger;
        ITwilioUtil _twilioUtil;
        IStripeUtil _stripeUtil;
        IHostingEnvironment _environment;
        private readonly IDbCommonRepository<DbShop> _shopRepository;
        private readonly ICustomerServiceHandler _customerServiceHandler;
        IMemoryCache _memoryCache;
        private readonly IDateTimeUtil _dateTimeUtil;
        IAmountCaculaterUtil _amountCaculaterV1;

        public TrRestaurantBookingServiceHandler(ITwilioUtil twilioUtil, IDbCommonRepository<TrDbRestaurant> restaurantRepository, IDbCommonRepository<TrDbRestaurantBooking> restaurantBookingRepository, IDbCommonRepository<DbCustomer> customerRepository,
            IDbCommonRepository<DbShop> shopRepository, ICustomerServiceHandler customerServiceHandler, IHostingEnvironment environment, IStripeUtil stripeUtil, IMemoryCache memoryCache,
            IDbCommonRepository<StripeCheckoutSeesion> stripeCheckoutSeesionRepository, IDateTimeUtil dateTimeUtil, IAmountCaculaterUtil amountCaculaterV1,
            ILogManager logger, IContentBuilder contentBuilder, IEmailUtil emailUtil)
        {
            _restaurantRepository = restaurantRepository;
            _restaurantBookingRepository = restaurantBookingRepository;
            _stripeCheckoutSeesionRepository = stripeCheckoutSeesionRepository;
            _customerRepository = customerRepository;
            _contentBuilder = contentBuilder;
            _emailUtil = emailUtil;
            _logger = logger;
            _twilioUtil = twilioUtil;
            _environment = environment;
            _shopRepository = shopRepository;
            _customerServiceHandler = customerServiceHandler;
            _stripeUtil = stripeUtil;
            _memoryCache = memoryCache;
            _dateTimeUtil = dateTimeUtil;
            _amountCaculaterV1 = amountCaculaterV1;
        }


        public async Task<TrDbRestaurantBooking> GetBooking(string id)
        {
            var Booking = await _restaurantBookingRepository.GetOneAsync(r => r.Id == id);
            return Booking;
        }

        public async Task<bool> CancelBooking(string bookingId, string detailId, int shopId, string userEmail)
        {
            var booking = await _restaurantBookingRepository.GetOneAsync(a => a.Id == bookingId);
            foreach (var item in booking.Details)
            {
                if (item.Status == OrderStatusEnum.Canceled) continue;
                if (item.Id == detailId)
                {
                    item.Status = OrderStatusEnum.Canceled;
                    SendCancelEmail(booking, item);
                }
            }
            booking.Status = OrderStatusEnum.Canceled;
            booking.Updated = _dateTimeUtil.GetCurrentTime();
            booking.Updater = userEmail;
            OperationInfo operationInfo = new OperationInfo() { ModifyType = 3, Operater = userEmail, UpdateTime = DateTime.Now, Operation = "订单取消" };
            booking.Operations.Add(operationInfo);

            var savedRestaurant = await _restaurantBookingRepository.UpdateAsync(booking);

            return savedRestaurant != null;
        }
        private async void SendCancelEmail(TrDbRestaurantBooking booking, BookingDetail detail)
        {
            var shopInfo = await _shopRepository.GetOneAsync(r => r.ShopId == booking.ShopId && r.IsActive.HasValue && r.IsActive.Value);
            if (shopInfo == null)
            {
                _logger.LogInfo("----------------Cannot find shop info" + booking.Id);
                throw new ServiceException("Cannot find shop info");
            }
            string currencyStr = booking.PayCurrency == "UK" ? "￡" : "€";
            decimal exRate = (decimal)((double)shopInfo.ExchangeRate);
            decimal amount = 0;
            decimal paidAmount = 0; detail.AmountInfos.Sum(x => x.PaidAmount);

            paidAmount = detail.AmountInfos.Sum(x => x.PaidAmount);
            if (booking.PayCurrency == detail.Currency)
            {
                amount = detail.AmountInfos.Sum(x => x.Amount);
            }
            else if (booking.PayCurrency == "UK")
                amount = detail.AmountInfos.Sum(x => x.Amount) * exRate;
            else
                amount = detail.AmountInfos.Sum(x => x.Amount) / exRate;
            paidAmount = Math.Round(paidAmount, 2);
            amount = Math.Round(amount, 2);
            string Detail = "";
            foreach (var course in detail.Courses)
            {
                Detail += $"{course.MenuItemName} * {course.Qty} 人 {currencyStr}{paidAmount}/{amount}<br>";
            }
            _twilioUtil.sendSMS(detail.RestaurantPhone, "You got a new order. Please see details in groupmeals.com");
            Detail += $"Amount(金额)：<b>{currencyStr}{amount}</b>, Paid(已付)：<b>{currencyStr}{paidAmount}</b>, UnPaid(待支付)：<b style=\"color: red;\">{currencyStr}{amount - paidAmount}</b>";
            var detailstr = new HtmlString(Detail);
            string htmlTemp = EmailTemplateUtil.ReadTemplate(this._environment.WebRootPath, "cancel_meals_restaurant");
            var emailHtml = await _contentBuilder.BuildRazorContent(new { booking, detail, Detail = detailstr, Memo = detail.Courses[0].Memo }, htmlTemp);
            try
            {
                BackgroundJob.Enqueue<ITourBatchServiceHandler>(s => s.SendEmail(shopInfo.ShopSettings, shopInfo.Email, detail.RestaurantEmail, "Order canceled", emailHtml));
                BackgroundJob.Enqueue<ITourBatchServiceHandler>(s => s.SendEmail(shopInfo.ShopSettings, shopInfo.Email, booking.CustomerEmail, "Order canceled", emailHtml));
            }
            catch (Exception ex)
            {
                _logger.LogError($"SendCancelEmail {ex.Message} -{ex.StackTrace} ");
            }

        }
        public async Task<bool> UpdateBooking(string billId, string productId, string priceId)
        {
            TrDbRestaurantBooking booking = GetBooking(billId).Result;
            if (booking == null) return false;
            booking.PaymentInfos[0].StripeProductId = productId;
            booking.PaymentInfos[0].StripePriceId = priceId;
            var temp = await _restaurantBookingRepository.UpdateAsync(booking);
            return true;
        }
        public async Task<object> UpdateAccepted(string billId, string subBillId, int acceptType, string operater)
        {
            TrDbRestaurantBooking DBbooking = GetBooking(billId).Result;
            TrDbRestaurantBooking booking = JsonConvert.DeserializeObject<TrDbRestaurantBooking>(JsonConvert.SerializeObject(DBbooking));// JsonConvert.DeserializeObject< TrDbRestaurantBooking >(JsonConvert.SerializeObject(DBbooking));
            foreach (var item in DBbooking.Details)
            {
                if (item.Id == subBillId)
                {
                    if (item.AcceptStatus == 0)
                        item.AcceptStatus = (AcceptStatusEnum)acceptType;
                    else if (item.AcceptStatus == AcceptStatusEnum.Accepted)
                    {
                        return new { code = 1, msg = "订单已接收", data = booking };
                    }
                    else if (item.AcceptStatus == AcceptStatusEnum.Declined)
                    {
                        return new { code = 2, msg = "订单已被拒绝", data = booking };
                    }
                }
            }
            if (booking == null) return false;
            //if (acceptType == 2)
            //{
            //    _stripeUtil.RefundGroupMeals(booking);
            //}
            var opt = new OperationInfo() { Operater = operater, Operation = acceptType == 1 ? "接收预订" : "拒绝预订", UpdateTime = DateTime.Now };
            booking.Operations.Add(opt);
            var temp = await _restaurantBookingRepository.UpdateAsync(DBbooking);
            string msg = $"您于{booking.BookingDate} {booking.BookingTime} 提交的订单已被接收，请按时就餐";
            if (acceptType == 2)
                msg = $"您于{booking.BookingDate} {booking.BookingTime} 提交的订单已被拒绝，请登录groupmeals.com查询别的餐厅";
            _twilioUtil.sendSMS(booking.CustomerPhone, msg);
            var shopInfo = await _shopRepository.GetOneAsync(r => r.ShopId == booking.ShopId && r.IsActive.HasValue && r.IsActive.Value);
            if (shopInfo == null)
            {
                _logger.LogInfo("----------------Cannot find shop info" + booking.Id);
                return new { code = 500, msg = "Cannot find shop info", };
            }
            booking.Details.Clear();
            foreach (var item in DBbooking.Details)
            {
                if (item.Id == subBillId)
                {
                    booking.Details.Add(item);
                }
            }
            if (acceptType == 1)
                BackgroundJob.Schedule(() =>
                    sendEmail(booking, shopInfo, "new_meals_confirm", this._environment.WebRootPath, "Your order has been accepted")
                , TimeSpan.FromMinutes(1));

            else if (acceptType == 2)
            {
                BackgroundJob.Schedule(() =>
                   sendEmail(booking, shopInfo, "new_meals_decline", this._environment.WebRootPath, "Your order has been declined")
              , TimeSpan.FromMinutes(1));

            }
            return new { code = 0, msg = "ok", data = booking };
        }
        public void sendEmail(TrDbRestaurantBooking _booking, DbShop shopInfo, string tempName, string wwwPath, string subject)
        {
            _sendEmail(_booking, shopInfo, tempName, wwwPath, subject);
        }
        public async void _sendEmail(TrDbRestaurantBooking _booking, DbShop shopInfo, string tempName, string wwwPath, string subject)
        {
            var booking = await _restaurantBookingRepository.GetOneAsync(a => a.Id == _booking.Id);
            EmailUtils.EmailCustomer(booking, shopInfo, tempName, wwwPath, subject, _contentBuilder, _logger);

            //var schedulerFactory = new StdSchedulerFactory();
            //var scheduler = await schedulerFactory.GetScheduler();
            //await scheduler.Start();

            ////创建作业和触发器
            //var jobDetail = JobBuilder.Create<QuartzTask>().SetJobData(new JobDataMap() {
            //                    new KeyValuePair<string, object>("BookingID", booking.Id),
            //                    new KeyValuePair<string, object>("ShopInfo", shopInfo),
            //                    new KeyValuePair<string, object>("TempName", tempName),
            //                    new KeyValuePair<string, object>("Subject", subject),
            //                    new KeyValuePair<string, object>("WwwPath", wwwPath),
            //                    new KeyValuePair<string, object>("ContentBuilder", _contentBuilder),
            //                    new KeyValuePair<string, object>("Logger", _logger),
            //                    new KeyValuePair<string, object>("RestaurantBookingRepository", _restaurantBookingRepository),
            //                }).Build();
            //var trigger = TriggerBuilder.Create()
            //                            .WithSimpleSchedule(m =>
            //                            {
            //                                m.WithRepeatCount(0).WithIntervalInSeconds(10);
            //                            }).StartAt(new DateTimeOffset(DateTime.Now.AddSeconds(20)))
            //                            .Build();

            ////添加调度
            //await scheduler.ScheduleJob(jobDetail, trigger);

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
            var opt = new OperationInfo() { Operater = operater, Operation = "添加原因", UpdateTime = DateTime.Now };
            booking.Operations.Add(opt);
            var temp = await _restaurantBookingRepository.UpdateAsync(booking);
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
                var temp = await _restaurantBookingRepository.UpdateAsync(booking);

                _logger.LogInfo("BookingPaid.UpdateAsync.session" + temp.Id);
                var newItem = await _stripeCheckoutSeesionRepository.CreateAsync(new StripeCheckoutSeesion() { Data = session, BookingId = booking.Id });
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
                }
                _logger.LogInfo("----------------BookingPaid" + booking.Id);
                var temp = await _restaurantBookingRepository.UpdateAsync(booking);
                ClearCart(temp);
                var shopInfo = await _shopRepository.GetOneAsync(r => r.ShopId == booking.ShopId && r.IsActive.HasValue && r.IsActive.Value);
                if (shopInfo == null)
                {
                    _logger.LogInfo("----------------Cannot find shop info" + booking.Id);
                    throw new ServiceException("Cannot find shop info");
                }

               

                EmailUtils.EmailCustomerTotal(booking, shopInfo, "new_meals", this._environment.WebRootPath, "Thank you for your Booking", _contentBuilder, _logger);
                EmailUtils.EmailBoss(booking, shopInfo, "new_meals_restaurant", this._environment.WebRootPath, "New Booking", _twilioUtil, _contentBuilder, _logger);
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
                _customerServiceHandler.UpdateCart(customer, (int)booking.ShopId);
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
        public async Task<ResponseModel> ModifyBooking(TrDbRestaurantBooking booking, int shopId, string email)
        {
            Guard.NotNull(booking);
            Guard.AreEqual(booking.ShopId.Value, shopId);
            var exsitBooking = await _restaurantBookingRepository.GetOneAsync(r => !r.IsDeleted && r.Id == booking.Id);
            if (exsitBooking == null) return new ResponseModel { msg = "booking not found", code = 200, data = null };
            OperationInfo operationInfo = new OperationInfo() { ModifyType = 4, Operater = email, UpdateTime = DateTime.Now, Operation = "订单修改" };
            bool isChange = false;
            foreach (var item in exsitBooking.Details)
            {
                var detail = booking.Details.FirstOrDefault(d => d.Id == item.Id);
                if (detail != null)
                {
                    if (detail.SelectDateTime != item.SelectDateTime)
                    {
                        item.Modified = true;
                        isChange = true;
                        ModifyInfo modifyInfo = new ModifyInfo();
                        modifyInfo.ModifyField = 1;
                        modifyInfo.ModifyLocation = $"{booking.Id}>{item.Id}";
                        modifyInfo.oldValue = item.SelectDateTime.ToString();
                        modifyInfo.newValue = detail.SelectDateTime.ToString();
                        operationInfo.ModifyInfos.Add(modifyInfo);
                        item.SelectDateTime = detail.SelectDateTime;
                    }
                    if (detail.Memo != item.Memo)
                    {
                        item.Modified = true;
                        isChange = true;
                        ModifyInfo modifyInfo = new ModifyInfo();
                        modifyInfo.ModifyField = 2;
                        modifyInfo.ModifyLocation = $"{booking.Id}>{item.Id}";
                        modifyInfo.oldValue = item.Memo;
                        modifyInfo.newValue = detail.Memo;
                        operationInfo.ModifyInfos.Add(modifyInfo);
                        item.Memo = detail.Memo;
                    }
                    var Oldamount = _amountCaculaterV1.getItemAmount(item);

                    foreach (var course in item.Courses)
                    {

                        var _course = detail.Courses.FirstOrDefault(c => c.Id == course.Id);
                        if (_course == null) continue;
                        if (course.Price == _course.Price && course.Qty == _course.Qty && course.ChildrenQty == _course.ChildrenQty) continue;
                        if (course.Qty != _course.Qty)
                        {
                            item.Modified = true;
                            isChange = true;
                            ModifyInfo modifyInfo = new ModifyInfo();
                            modifyInfo.ModifyField = 3;
                            modifyInfo.ModifyLocation = $"{booking.Id}>{item.Id}>{course.Id}";
                            modifyInfo.oldValue = course.Qty.ToString();
                            modifyInfo.newValue = _course.Qty.ToString();
                            operationInfo.ModifyInfos.Add(modifyInfo);
                            course.Qty = _course.Qty;

                        }
                        if (course.ChildrenQty != _course.ChildrenQty)
                        {
                            item.Modified = true;
                            isChange = true;
                            ModifyInfo modifyInfo = new ModifyInfo();
                            modifyInfo.ModifyField = 5;
                            modifyInfo.ModifyLocation = $"{booking.Id}>{item.Id}>{course.Id}";
                            modifyInfo.oldValue = course.ChildrenQty.ToString();
                            modifyInfo.newValue = _course.ChildrenQty.ToString();
                            operationInfo.ModifyInfos.Add(modifyInfo);
                            course.ChildrenQty = _course.ChildrenQty;

                        }
                        if (course.Price != _course.Price)
                        {
                            item.Modified = true;
                            isChange = true;
                            ModifyInfo modifyInfo = new ModifyInfo();
                            modifyInfo.ModifyField = 4;
                            modifyInfo.ModifyLocation = $"{booking.Id}>{item.Id}>{course.Id}";
                            modifyInfo.oldValue = course.Price.ToString();
                            modifyInfo.newValue = _course.Price.ToString();
                            operationInfo.ModifyInfos.Add(modifyInfo);
                            course.Price = _course.Price;
                            course.Category = _course.Category;
                            course.CategoryId = _course.CategoryId;
                            course.MenuItemDescription = _course.MenuItemDescription;
                            course.MenuItemDescriptionCn = _course.MenuItemDescriptionCn;
                            course.MenuItemNameCn = _course.MenuItemNameCn;
                            course.MenuItemName = _course.MenuItemName;
                            course.Cuisine = _course.Cuisine;
                            course.Ingredieent = _course.Ingredieent;
                        }
                    }
                    var amount = _amountCaculaterV1.getItemAmount(item);
                    if (amount != Oldamount)
                    {
                        AmountInfo amountInfo = new AmountInfo() { Id = "A" + SnowflakeId.getSnowId() };
                        amountInfo.Amount = amount - Oldamount;//新增差价记录
                        item.AmountInfos.Add(amountInfo);
                    }
                }
            }
            if (isChange)
            {
                var temo = _amountCaculaterV1.CalculateOrderPaidAmount(exsitBooking, 0.8);
                exsitBooking.Operations.Add(operationInfo);
                var savedBooking = await _restaurantBookingRepository.UpdateAsync(exsitBooking);
                TrDbRestaurantBooking sendBooking = JsonConvert.DeserializeObject<TrDbRestaurantBooking>(JsonConvert.SerializeObject(savedBooking));
                sendBooking.Details.Clear();
                foreach (var item in exsitBooking.Details)
                {
                    if (item.Modified)
                        sendBooking.Details.Add(item);
                }
                SendModifyEmail(sendBooking);
            }
            return new ResponseModel { msg = "", code = 200, data = null };
        }



        public async Task<ResponseModel> RequestBooking(TrDbRestaurantBooking booking, int shopId, string userId)
        {
            _logger.LogInfo("RequestBooking" + shopId);
            Guard.NotNull(booking);
            Guard.AreEqual(booking.ShopId.Value, shopId);

            DbCustomer user = await _customerRepository.GetOneAsync(a => a.Id == userId);

            foreach (var item in booking.Details)
            {
                var rest = await _restaurantRepository.GetOneAsync(a => a.Id == item.RestaurantId);
                item.RestaurantName = rest.StoreName;
                item.RestaurantEmail = rest.Email;
                item.RestaurantAddress = rest.Address;
                item.RestaurantPhone = rest.PhoneNumber;
                if ((item.SelectDateTime - DateTime.Now).Value.TotalHours < 12)
                {
                    return new ResponseModel { msg = "用餐时间少于12个小时", code = 200, data = null };
                }
            }
            TourBooking newBooking;
            var createTime = _dateTimeUtil.GetCurrentTime();
            var exsitBooking = await _restaurantBookingRepository.GetOneAsync(r => r.Status == OrderStatusEnum.None && !r.IsDeleted && r.CustomerEmail == user.Email);
            var tem = await _restaurantBookingRepository.GetManyAsync(a => 1 == 1);
            var te = tem.ToList();
            //var exsitBooking = exsitBookings.FirstOrDefault();// (a => (createTime - a.Created).Value.Hours < 2);
            if (exsitBooking != null)
            {
                bool noPay = true; ;
                foreach (var item in booking.Details)
                {
                    if (item.BillInfo.PaymentType != PaymentTypeEnum.PayAtStore)
                        noPay = false;
                }
                if (noPay)
                {
                    booking.Status = OrderStatusEnum.UnAccepted;
                    //SendEmail(booking);

                }
                exsitBooking.Created = _dateTimeUtil.GetCurrentTime();
                exsitBooking.Details = booking.Details;
                exsitBooking.Status = booking.Status;
                exsitBooking.CustomerName = user.UserName;
                exsitBooking.CustomerPhone = user.Phone;
                exsitBooking.CustomerEmail = user.Email;
                exsitBooking.PayCurrency = booking.PayCurrency;
                var savedBooking = await _restaurantBookingRepository.UpdateAsync(exsitBooking);
                if (noPay)
                {
                    ClearCart(savedBooking);
                    SendEmail(savedBooking);
                }
                return new ResponseModel { msg = "", code = 200, data = savedBooking };
            }
            else
            {
                bool noPay = true; ;
                foreach (var item in booking.Details)
                {
                    item.Id = Guid.NewGuid().ToString();
                    if (item.BillInfo.PaymentType != PaymentTypeEnum.PayAtStore)
                        noPay = false;
                    foreach (var course in item.Courses)
                    {
                        course.Id = "C" + SnowflakeId.getSnowId();
                    }
                    AmountInfo amountInfo = new AmountInfo()
                    {
                        Id = "A" + SnowflakeId.getSnowId(),
                        Amount = _amountCaculaterV1.getItemAmount(item),
                        PaidAmount = _amountCaculaterV1.getItemPayAmount(item)
                    };
                    if(item.AmountInfos.Count()==0)
                    item.AmountInfos.Add(amountInfo);
                    foreach (var amount in item.AmountInfos)
                    {
                        amount.Id = "A" + SnowflakeId.getSnowId();

                    }
                }
                if (noPay)
                {
                    booking.Status = OrderStatusEnum.UnAccepted;
                }
                booking.Id = Guid.NewGuid().ToString();
                booking.BookingRef = "GM" + SnowflakeId.getSnowId();
                booking.Created = _dateTimeUtil.GetCurrentTime();
                var opt = new OperationInfo() { Operater = user.Email, Operation = "新增订单", UpdateTime = DateTime.Now };
                booking.Operations.Add(opt);

                var shopInfo = await _shopRepository.GetOneAsync(r => r.ShopId == booking.ShopId && r.IsActive.HasValue && r.IsActive.Value);
                if (shopInfo == null)
                {
                    _logger.LogInfo("----------------Cannot find shop info" + booking.Id);
                    return new ResponseModel { msg = "Cannot find shop info", code = 500,  };
                }

                double exchange = (double)shopInfo.ExchangeRate;
                booking.PaymentInfos.Add(new PaymentInfo() { Amount = _amountCaculaterV1.CalculateOrderAmount(booking, exchange), PaidAmount = _amountCaculaterV1.CalculateOrderPaidAmount(booking, exchange) });
                booking.CustomerName = user.UserName;
                booking.CustomerPhone = user.Phone;
                booking.CustomerEmail = user.Email;
                var newItem = await _restaurantBookingRepository.CreateAsync(booking);
                if (noPay)
                {
                    ClearCart(newItem);
                    SendEmail(newItem);
                }

                return new ResponseModel { msg = "", code = 200, data = newItem };
            }
        }
        private async void SendEmail(TrDbRestaurantBooking booking)
        {
            var shopInfo = await _shopRepository.GetOneAsync(r => r.ShopId == booking.ShopId && r.IsActive.HasValue && r.IsActive.Value);
            if (shopInfo == null)
            {
                _logger.LogInfo("----------------Cannot find shop info" + booking.Id);
                throw new ServiceException("Cannot find shop info");
            }

            EmailUtils.EmailCustomerTotal(booking, shopInfo, "new_meals", this._environment.WebRootPath, "Thank you for your Booking", _contentBuilder, _logger);
            EmailUtils.EmailBoss(booking, shopInfo, "new_meals_restaurant", this._environment.WebRootPath, "New Booking", _twilioUtil, _contentBuilder, _logger);
            //EmailUtils.EmailSupport(booking, shopInfo, "new_meals_support", this._environment.WebRootPath, "New Booking", _twilioUtil, _contentBuilder, exchange, _logger);

        }
        private async void SendModifyEmail(TrDbRestaurantBooking booking)
        {
            var shopInfo = await _shopRepository.GetOneAsync(r => r.ShopId == booking.ShopId && r.IsActive.HasValue && r.IsActive.Value);
            if (shopInfo == null)
            {
                _logger.LogInfo("----------------Cannot find shop info" + booking.Id);
                throw new ServiceException("Cannot find shop info");
            }


            EmailUtils.EmailCustomerTotal(booking, shopInfo, "new_modify", this._environment.WebRootPath, "Booking Modified", _contentBuilder, _logger);
            EmailUtils.EmailBoss(booking, shopInfo, "new_modify", this._environment.WebRootPath, "Booking Modified", _twilioUtil, _contentBuilder, _logger);
            //EmailUtils.EmailSupport(booking, shopInfo, "new_modify", this._environment.WebRootPath, "Booking Modified", _twilioUtil, _contentBuilder, exchange, _logger);

        }

        //private async Task EmailBoss(TrDbRestaurantBooking booking, DbShop shopInfo, string subject)
        //{
        //    string wwwPath = this._environment.WebRootPath;
        //    string htmlTemp = EmailTemplateUtil.ReadTemplate(wwwPath, "new_meals_restaurant");
        //    string Detail = "";
        //    foreach (var item in booking.Details)
        //    {
        //        foreach (var course in item.Courses)
        //        {
        //            Detail += course.MenuItemName + " * " + course.Qty + "人  €" + course.Amount;
        //        }
        //    }
        //    _twilioUtil.sendSMS(booking.Details[0].RestaurantPhone, "You got a new order. Please see details in groupmeals.com");
        //    var emailHtml = await _contentBuilder.BuildRazorContent(new { booking = booking, details = booking.Details[0], Detail, Memo = booking.Details[0].Courses[0].Memo }, htmlTemp);

        //    try
        //    {
        //        BackgroundJob.Enqueue<ITourBatchServiceHandler>(
        //            s => s.SendEmail(shopInfo.ShopSettings, shopInfo.Email, booking.Details[0].RestaurantEmail, subject,
        //                emailHtml));

        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError($"EmailBossError {ex.Message} -{ex.StackTrace} ");
        //    }
        //}

        //private async Task EmailCustomer(TrDbRestaurantBooking booking, DbShop shopInfo, string tempName)
        //{
        //    string wwwPath = this._environment.WebRootPath;
        //    string htmlTemp = EmailTemplateUtil.ReadTemplate(wwwPath, tempName);
        //    string Detail = "";
        //    foreach (var item in booking.Details)
        //    {
        //        Detail += item.RestaurantName + "       ";
        //        foreach (var course in item.Courses)
        //        {
        //            Detail += course.MenuItemName + " * " + course.Qty + "人  €" + course.Amount;
        //        }
        //    }
        //    var emailHtml = "";
        //    try
        //    {
        //        emailHtml = await _contentBuilder.BuildRazorContent(new { booking = booking.Details[0], Detail }, htmlTemp);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError("----------------emailHtml---error" + ex.Message);
        //    }
        //    if (string.IsNullOrWhiteSpace(emailHtml))
        //    {
        //        return;
        //    }
        //    try
        //    {
        //        BackgroundJob.Enqueue<ITourBatchServiceHandler>(
        //            s => s.SendEmail(shopInfo.ShopSettings, shopInfo.Email, booking.CustomerEmail, $"Thank you for your Booking",
        //                emailHtml));

        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError($"EmailCustomer Email Customer Error {ex.Message} -{ex.StackTrace} ");
        //    }
        //}
        public async Task<bool> ResendEmail(string bookingId)
        {
            try
            {
                TrDbRestaurantBooking booking = await _restaurantBookingRepository.GetOneAsync(r => r.Id == bookingId);
                if (booking != null)
                {
                    var shopInfo = await _shopRepository.GetOneAsync(r => r.ShopId == booking.ShopId && r.IsActive.HasValue && r.IsActive.Value);
                    if (shopInfo == null)
                    {
                        throw new ServiceException("Cannot find shop info");
                    }
                    EmailUtils.EmailBoss(booking, shopInfo, "new_meals_restaurant", this._environment.WebRootPath, "New Booking", _twilioUtil, _contentBuilder, _logger);
                    //EmailUtils.EmailCustomerTotal(booking, shopInfo, "new_meals", this._environment.WebRootPath, "New Booking", _contentBuilder, 1, _logger);
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
            var temp = await _restaurantBookingRepository.UpdateAsync(booking);

            //var newItem = await _stripeCheckoutSeesionRepository.CreateAsync(new StripeCheckoutSeesion() { Data = session, BookingId = booking.Id });

            return true;
        }
        public async Task<TrDbRestaurantBooking> BindingPayInfoToTourBooking(TrDbRestaurantBooking booking, string PaymentId, string stripeClientSecretKey, bool isSetupPay)
        {
            Guard.NotNull(booking);
            booking.PaymentInfos[0].StripePaymentId = PaymentId;
            booking.PaymentInfos[0].SetupPay = isSetupPay;
            booking.PaymentInfos[0].StripeClientSecretKey = stripeClientSecretKey;
            var res = await _restaurantBookingRepository.UpdateAsync(booking);
            return res;
        }
        public async Task<bool> DeleteBooking(string bookingId, int shopId)
        {
            var booking = await _restaurantBookingRepository.GetOneAsync(a => a.Id == bookingId);
            booking.IsDeleted = true; booking.Updated = _dateTimeUtil.GetCurrentTime();
            var savedRestaurant = await _restaurantBookingRepository.UpdateAsync(booking);
            return savedRestaurant != null;
        }
        public async Task<ResponseModel> SearchBookings(int shopId, string email, string content, int pageSize = -1, string continuationToken = null)
        {
            //settleOrder();
            if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(content))
            {
                var Bookings = await _restaurantBookingRepository.GetManyAsync(a => (a.Status != OrderStatusEnum.None && !a.IsDeleted) || a.Details.Any(b => b.RestaurantEmail == email), pageSize, continuationToken);
                var list = Bookings.Value.ToList();
                return new ResponseModel { msg = "ok", code = 200, token = Bookings.Key, data = list };
            }
            if (string.IsNullOrWhiteSpace(content))
            {
                var Bookings = await _restaurantBookingRepository.GetManyAsync(a => (a.Status != OrderStatusEnum.None && !a.IsDeleted) || a.Details.Any(b => b.RestaurantEmail == email), pageSize, continuationToken);
                var list = Bookings.Value.ToList().FindAll(a => a.CustomerEmail == email).ToList();
                return new ResponseModel { msg = "ok", code = 200, token = Bookings.Key, data = list };
            }

            else
            {
                var Bookings = await _restaurantBookingRepository.GetManyAsync(a => (a.Status != OrderStatusEnum.None && !a.IsDeleted) || a.Details.Any(b => b.RestaurantEmail == email), pageSize, continuationToken);
                var list = Bookings.Value.ToList().FindAll(a => a.Details.Any(d => d.RestaurantName.ToLower().Contains(content.ToLower()))).ToList();
                return new ResponseModel { msg = "ok", code = 200, token = Bookings.Key, data = list };
            }

        }
        public async void SettleOrder()
        {
            DateTime stime = DateTime.Now;
            var Bookings = await _restaurantBookingRepository.GetManyAsync(a => (a.Status != OrderStatusEnum.None && !a.IsDeleted));
            var span = (DateTime.Now - stime).TotalMilliseconds;
            Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " : " + span);
            var list = Bookings.ToList();
            foreach (var item in list)
            {
                bool isSettled = true;
                foreach (var b in item.Details)
                {
                    if (b.Status == OrderStatusEnum.Accepted && b.AcceptStatus == AcceptStatusEnum.Accepted && b.SelectDateTime < DateTime.Now)
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
                    item.Details.ForEach(a => a.Status = OrderStatusEnum.Settled);
                    await _restaurantBookingRepository.UpdateAsync(item);
                }
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

    }
}
