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
        Task<bool> UpdateAccepted(string bookingId, string subBillId, int acceptType, string operater);
        Task<bool> UpdateAcceptedReason(string bookingId, string subBillId, string reason, string operater);
        Task<bool> CancelBooking(string bookingId, string detailId, int shopId);
    }
    public class TrRestaurantBookingServiceHandler : ITrRestaurantBookingServiceHandler
    {

        private readonly IDbCommonRepository<TrDbRestaurantBooking> _restaurantBookingRepository;
        private readonly IDbCommonRepository<StripeCheckoutSeesion> _stripeCheckoutSeesionRepository;
        private readonly IContentBuilder _contentBuilder;
        private readonly IEmailUtil _emailUtil;
        ILogManager _logger;
        ITwilioUtil _twilioUtil;
        IStripeUtil _stripeUtil;
        IHostingEnvironment _environment;
        private readonly IDbCommonRepository<DbShop> _shopRepository;
        IMemoryCache _memoryCache;
        private readonly IDateTimeUtil _dateTimeUtil;

        public TrRestaurantBookingServiceHandler(ITwilioUtil twilioUtil, IDbCommonRepository<TrDbRestaurantBooking> restaurantBookingRepository,
            IDbCommonRepository<DbShop> shopRepository, IHostingEnvironment environment, IStripeUtil stripeUtil, IMemoryCache memoryCache,
            IDbCommonRepository<StripeCheckoutSeesion> stripeCheckoutSeesionRepository, IDateTimeUtil dateTimeUtil,
            ILogManager logger, IContentBuilder contentBuilder, IEmailUtil emailUtil)
        {
            _restaurantBookingRepository = restaurantBookingRepository;
            _stripeCheckoutSeesionRepository = stripeCheckoutSeesionRepository;
            _contentBuilder = contentBuilder;
            _emailUtil = emailUtil;
            _logger = logger;
            _twilioUtil = twilioUtil;
            _environment = environment;
            _shopRepository = shopRepository;
            _stripeUtil = stripeUtil;
            _memoryCache = memoryCache;
            _dateTimeUtil = dateTimeUtil;
        }


        public async Task<TrDbRestaurantBooking> GetBooking(string id)
        {
            var Booking = await _restaurantBookingRepository.GetOneAsync(r => r.Id == id);
            return Booking;
        }

        public async Task<bool> CancelBooking(string bookingId, string detailId, int shopId)
        {
            var booking = await _restaurantBookingRepository.GetOneAsync(a => a.Id == bookingId);
            foreach (var item in booking.Details)
            {
                if (item.Id == detailId)
                {
                    item.Status = 1;
                    SendCancelEmail(booking, item);
                }
            }
            booking.Status = OrderStatusEnum.PartialCanceled;
            booking.Updated = _dateTimeUtil.GetCurrentTime();
            var savedRestaurant = await _restaurantBookingRepository.UpdateAsync(booking);

            return savedRestaurant != null;
        }
        private async void SendCancelEmail(TrDbRestaurantBooking booking, BookingDetail detail)
        {
            var shopInfo = await _shopRepository.GetOneAsync(r => r.ShopId == 13 && r.IsActive.HasValue && r.IsActive.Value);
            if (shopInfo == null)
            {
                _logger.LogInfo("----------------Cannot find shop info" + booking.Id);
                throw new ServiceException("Cannot find shop info");
            }
            string currencyStr = booking.PayCurrency == "UK" ? "￡" : "€";
            decimal exRate = (decimal)((double)_memoryCache.Get("ExchangeRate"));
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
        public async Task<bool> UpdateAccepted(string billId, string subBillId, int acceptType, string operater)
        {
            TrDbRestaurantBooking booking = GetBooking(billId).Result;
            foreach (var item in booking.Details)
            {
                if (item.Id == subBillId)
                {
                    item.AcceptStatus = acceptType;
                }
            }
            if (booking == null) return false;
            //if (acceptType == 2)
            //{
            //    _stripeUtil.RefundGroupMeals(booking);
            //}
            var opt = new OperationInfo() { Operater = operater, Operation = acceptType == 1 ? "接收预订" : "拒绝预订", UpdateTime = DateTime.Now };
            booking.Operations.Add(opt);
            var temp = await _restaurantBookingRepository.UpdateAsync(booking);
            string msg = $"您于{booking.BookingDate} {booking.BookingTime} 提交的订单已被接收，请按时就餐";
            if (acceptType == 2)
                msg = $"您于{booking.BookingDate} {booking.BookingTime} 提交的订单已被拒绝，请登录groupmeals.com查询别的餐厅";
            //_twilioUtil.sendSMS(booking.CustomerPhone, msg);
            var shopInfo = await _shopRepository.GetOneAsync(r => r.ShopId == 13 && r.IsActive.HasValue && r.IsActive.Value);
            if (shopInfo == null)
            {
                _logger.LogInfo("----------------Cannot find shop info" + booking.Id);
                throw new ServiceException("Cannot find shop info");
            }
            if (acceptType == 1)
                sendEmail(booking, shopInfo, "new_meals_confirm", this._environment.WebRootPath, _contentBuilder, _logger);
            else if (acceptType == 2)
            {
                sendEmail(booking, shopInfo, "new_meals_decline", this._environment.WebRootPath, _contentBuilder, _logger);
            }
            return true;
        }

        public async void sendEmail(TrDbRestaurantBooking booking, DbShop shopInfo, string tempName, string wwwPath, IContentBuilder _contentBuilder, ILogManager _logger)
        {
            var schedulerFactory = new StdSchedulerFactory();
            var scheduler = await schedulerFactory.GetScheduler();
            await scheduler.Start();

            //创建作业和触发器
            var jobDetail = JobBuilder.Create<QuartzTask>().SetJobData(new JobDataMap() {
                                new KeyValuePair<string, object>("BookingID", booking.Id),
                                new KeyValuePair<string, object>("ShopInfo", shopInfo),
                                new KeyValuePair<string, object>("TempName", tempName),
                                new KeyValuePair<string, object>("WwwPath", wwwPath),
                                new KeyValuePair<string, object>("ContentBuilder", _contentBuilder),
                                new KeyValuePair<string, object>("Logger", _logger),
                                new KeyValuePair<string, object>("RestaurantBookingRepository", _restaurantBookingRepository),
                            }).Build();
            var trigger = TriggerBuilder.Create()
                                        .WithSimpleSchedule(m =>
                                        {
                                            m.WithRepeatCount(0).WithIntervalInSeconds(10);
                                        }).StartAt(new DateTimeOffset(DateTime.Now.AddSeconds(20)))
                                        .Build();

            //添加调度
            await scheduler.ScheduleJob(jobDetail, trigger);

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
                    booking.Status = Domain.Enum.OrderStatusEnum.Paid;
                }
                _logger.LogInfo("----------------BookingPaid" + booking.Id);
                var temp = await _restaurantBookingRepository.UpdateAsync(booking);
                var shopInfo = await _shopRepository.GetOneAsync(r => r.ShopId == 13 && r.IsActive.HasValue && r.IsActive.Value);
                if (shopInfo == null)
                {
                    _logger.LogInfo("----------------Cannot find shop info" + booking.Id);
                    throw new ServiceException("Cannot find shop info");
                }

                decimal exchange = 1;
                try
                {
                    exchange = (decimal)((double)_memoryCache.Get("ExchangeRate"));
                }
                catch (Exception ex)
                {

                    _logger.LogError("----------------_memoryCache.get.excahngeRate.err" + ex.Message + "： " + ex.StackTrace);
                }

                EmailUtils.EmailCustomerTotal(booking, shopInfo, "new_meals", this._environment.WebRootPath, _contentBuilder, exchange, _logger);
                EmailUtils.EmailBoss(booking, shopInfo, "New Order", this._environment.WebRootPath, _twilioUtil, _contentBuilder, exchange, _logger);
                //var newItem = await _stripeCheckoutSeesionRepository.CreateAsync(new StripeCheckoutSeesion() { Data = session, BookingId = booking.Id });
            }
            catch (Exception ex)
            {
                _logger.LogInfo("----------------BookingPaid.err" + ex.Message + "： " + ex.StackTrace);
            }
            return true;
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
                    var shopInfo = await _shopRepository.GetOneAsync(r => r.ShopId == 13 && r.IsActive.HasValue && r.IsActive.Value);
                    if (shopInfo == null)
                    {
                        throw new ServiceException("Cannot find shop info");
                    }
                    EmailUtils.EmailCustomerTotal(booking, shopInfo, "new_meals", this._environment.WebRootPath, _contentBuilder, 1, _logger);
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

    }
}
