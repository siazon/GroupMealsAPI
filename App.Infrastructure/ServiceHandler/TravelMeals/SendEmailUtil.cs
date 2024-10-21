using App.Domain.Common;
using App.Domain.Common.Customer;
using App.Domain.Common.Email;
using App.Domain.Common.Shop;
using App.Domain.Enum;
using App.Domain.TravelMeals;
using App.Infrastructure.Builders.Common;
using App.Infrastructure.Exceptions;
using App.Infrastructure.Extensions;
using App.Infrastructure.Repository;
using App.Infrastructure.ServiceHandler.Common;
using App.Infrastructure.ServiceHandler.Tour;
using App.Infrastructure.Utility.Common;
using Hangfire;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.Azure.Cosmos;
using Quartz.Impl.Triggers;
using Stripe;
using Stripe.FinancialConnections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;

namespace App.Infrastructure.ServiceHandler.TravelMeals
{

    public interface ISendEmailUtil
    {
        Task EmailVerifyCode(string email, string code, DbShop shopInfo, string tempName, string wwwPath, string subject, string title, string titleCN);
        Task<bool> EmailEach(List<DbBooking> bookings, DbShop shopInfo, EmailSenderParams senderParams);
        Task<bool> EmailGroup(List<DbBooking> bookings, DbShop shopInfo, EmailSenderParams senderParams, DbCustomer user);
        Task EmailCustomer(DbBooking booking, DbShop shopInfo, string tempName, string wwwPath, string subject);
        Task<bool> SendModifiedEmail(DbBooking booking, DbShop shopInfo, string tempName, string wwwPath, string subject);
        Task SendCancelEmail(DbShop shopInfo, DbBooking detail, string webPath, string tempName, string subject, params string[] ccEmail);
    }
    public class SendEmailUtil : ISendEmailUtil
    {

        private readonly IEmailUtil _emailUtil;
        ILogManager _logger;
        IHostingEnvironment _environment;
        IContentBuilder _contentBuilder;
        ICountryServiceHandler _coutryHandler;
        private readonly IDateTimeUtil _dateTimeUtil;
        IAmountCalculaterUtil _amountCalculaterUtil;
        IOperationServiceHandler _operationServiceHandler;
        IMsgPusherServiceHandler _msgPusherServiceHandler;
        private readonly ICountryServiceHandler _countryHandler;
        private readonly IDbCommonRepository<DbPaymentInfo> _paymentRepository;

        public SendEmailUtil(IEmailUtil emailUtil, IAmountCalculaterUtil amountCalculaterUtil, ILogManager logger, IDateTimeUtil dateTimeUtil, ICountryServiceHandler coutryHandler, IHostingEnvironment environment,
     ICountryServiceHandler countryHandler, IDbCommonRepository<DbPaymentInfo> paymentRepository, IMsgPusherServiceHandler msgPusherServiceHandler, IOperationServiceHandler operationServiceHandler, IContentBuilder contentBuilder)
        {
            _emailUtil = emailUtil;
            _logger = logger;
            _environment = environment;
            _contentBuilder = contentBuilder;
            _dateTimeUtil = dateTimeUtil;
            _coutryHandler = coutryHandler;
            _amountCalculaterUtil = amountCalculaterUtil;
            _operationServiceHandler = operationServiceHandler;
            _msgPusherServiceHandler = msgPusherServiceHandler;
            _paymentRepository = paymentRepository;
            _countryHandler = countryHandler;
        }
        public async Task EmailVerifyCode(string email, string code, DbShop shopInfo, string tempName, string wwwPath, string subject, string title, string titleCN)
        {
            string htmlTemp = EmailTemplateUtil.ReadTemplate(wwwPath, tempName);

            var emailHtml = await _contentBuilder.BuildRazorContent(new { code, title, titleCN }, htmlTemp);
            try
            {
                _emailUtil.SendEmail(shopInfo.ShopSettings, shopInfo.Email, email, subject, emailHtml);
            }
            catch (Exception ex)
            {
                _logger.LogError($"EmailCustomer Email Customer Error {ex.Message} -{ex.StackTrace} ");
            }
        }

        public async Task<bool> EmailEach(List<DbBooking> bookings, DbShop shopInfo, EmailSenderParams senderParams)
        {
            var country = await _countryHandler.GetCountry(shopInfo.ShopId ?? 11);
            string Detail = "";
            foreach (var item in bookings)
            {
                decimal paidAmount = item.AmountInfos.Sum(x => x.PaidAmount);
                decimal amount = item.AmountInfos.Sum(x => x.Amount);
                decimal reward = item.AmountInfos.Sum(x => x.Reward);
                paidAmount = Math.Round(paidAmount, 2);
                amount = Math.Round(amount, 2);
                reward = Math.Round(reward, 2);
                Detail = "";
                string restaurantInfo = await AppendRestaurantInfo(item, senderParams.isShortInfo);
                string customerInfo = await AppendCustomerInfo(item, senderParams.isShortInfo);
                string selectDateTimeStr = item.SelectDateTime.Value.GetLocaTimeByIANACode(item.RestaurantTimeZone).ToString("yyyy-MM-dd HH:mm");
                if (senderParams.TemplateName == EmailConfigs.Instance.Emails[EmailTypeEnum.MealModified].TemplateName)
                {
                    Detail += restaurantInfo;
                    Detail += "<br>" + selectDateTimeStr + " <br><br> ";
                }

                if (senderParams.TemplateName != EmailConfigs.Instance.Emails[EmailTypeEnum.NewMealRestaurant].TemplateName)
                {
                    Detail += customerInfo;
                }
                List<string> names = new List<string>();
                foreach (var course in item.Courses)
                {
                    int qty = course.Qty + course.ChildrenQty;
                    Detail += $"{course.MenuItemName}({course.Price}) * {qty} 人 ";
                }
                string itemCurrencyStr = country.FirstOrDefault(a => a.Currency == item.Currency).CurrencySymbol;
                //_twilioUtil.sendSMS(item.RestaurantPhone, "You got a new order. Please see details in groupmeals.com");
                Detail += $"<br> Amount(金额)：<b>{itemCurrencyStr}{item.AmountInfos.Sum(x => x.Amount)}</b>, <br> Paid(已付)：<b>{itemCurrencyStr}{paidAmount}</b>,<br>";
                if (amount - paidAmount > 0)
                    Detail += $"<b style=\"color: red;\"> UnPaid(待支付)：{itemCurrencyStr}{amount - reward - paidAmount}</b>";
                senderParams.BookingRef = item.BookingRef;
                senderParams.BookingId = item.Id;
                senderParams.PaidAmount = itemCurrencyStr + ": " + Math.Round(paidAmount, 2) + "（立减：" + reward + ")";
                senderParams.UnPaidAmount = itemCurrencyStr + ": " + Math.Round((amount - reward - paidAmount), 2);
                senderParams.Amount = itemCurrencyStr + ": " + Math.Round(amount, 2);
                senderParams.MealTime = selectDateTimeStr;
                senderParams.Memo = item.Memo;
                senderParams.RestaurantInfo = restaurantInfo;
                senderParams.CustomerInfo = customerInfo;
                senderParams.Details = Detail;
                senderParams.ShopSettings = shopInfo.ShopSettings;
                await Send(senderParams);
            }
            return true;
        }
        private async Task<bool> Send(EmailSenderParams senderParams)
        {
            //Task.Run(async () =>
            //{

            string emailHtml = "";
            try
            {
                senderParams.Details = new HtmlString(senderParams.Details?.ToString());
                senderParams.RestaurantInfo = new HtmlString(senderParams.RestaurantInfo?.ToString());
                senderParams.CustomerInfo = new HtmlString(senderParams.CustomerInfo?.ToString());
                string htmlTemp = EmailTemplateUtil.ReadTemplate(this._environment.WebRootPath, senderParams.TemplateName);
                emailHtml = await _contentBuilder.BuildRazorContent(senderParams, htmlTemp);
            }
            catch (Exception ex)
            {
                _logger.LogError($"------{senderParams.TemplateName}-{senderParams.Subject}-emailHtml---error" + ex.Message);
            }
            try
            {
                await _emailUtil.SendEmail(senderParams.ShopSettings, senderParams.SenderEmail, senderParams.ReceiverEmail, senderParams.Subject, emailHtml, senderParams.CCEmail.ToArray());

            }
            catch (Exception ex)
            {
                _logger.LogError($"Email Error {ex.Message} -{ex.StackTrace} ");
            }
            return true;
            //});
        }
        private void SendToTalCustomer(DbShop shopInfo, DbCustomer user, string subject, string wwwPath, string tempName, string AmountStr, string PaidAmountStr, string UnpaidAmountStr, HtmlString detailstr)
        {

            Task.Run(async () =>
            {
                var emailHtml = "";
                try
                {

                    string htmlTemp = EmailTemplateUtil.ReadTemplate(wwwPath, tempName);
                    emailHtml = await _contentBuilder.BuildRazorContent(new { AmountStr, PaidAmountStr, UnpaidAmountStr, Detail = detailstr }, htmlTemp);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"------EmailCustomerTotal--------{tempName}-{subject}-emailHtml---error" + ex.Message);
                }
                if (string.IsNullOrWhiteSpace(emailHtml))
                {
                    return;
                }
                try
                {
                    await _emailUtil.SendEmail(shopInfo.ShopSettings, shopInfo.Email, user.Email, subject, emailHtml);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"EmailCustomer Email Customer Error {ex.Message} -{ex.StackTrace} ");
                }
            });
        }

        public async Task<bool> EmailGroup(List<DbBooking> bookings, DbShop shopInfo, EmailSenderParams senderParams, DbCustomer user)
        {
            string Detail = "";
            var country = await _countryHandler.GetCountry(11);
            foreach (var item in bookings)
            {
                if (item.Status == OrderStatusEnum.Canceled) continue;
                if (senderParams.TemplateName != EmailConfigs.Instance.Emails[EmailTypeEnum.NewMealCustomer].TemplateName)
                    Detail += await AppendRestaurantInfo(item);
                string selectDateTimeStr = item.SelectDateTime.Value.GetLocaTimeByIANACode(item.RestaurantTimeZone).ToString("yyyy-MM-dd HH:mm");
                Detail += selectDateTimeStr + " <br><br> ";

                Detail += await AppendCustomerInfo(item);
                Detail += item.RestaurantName;
                decimal amount = item.AmountInfos.Sum(x => x.Amount);
                decimal paidAmount = item.AmountInfos.Sum(x => x.PaidAmount);
                decimal reward = item.AmountInfos.Sum(x => x.Reward);
                SaveMsgPush(item, selectDateTimeStr);

                var itemCurrencyStr = country.FirstOrDefault(a => a.Currency == item.Currency).CurrencySymbol;
                List<string> names = new List<string>();
                foreach (var course in item.Courses)
                {
                    int qty = course.Qty + course.ChildrenQty;
                    Detail += $"{course.MenuItemName}({itemCurrencyStr} {course.Price}) * {qty} 人 ";
                }
                Detail += $"<br> Amount(金额)：{itemCurrencyStr}{Math.Round(amount, 2)}，    Paid(已付){itemCurrencyStr}" +
                    $"{Math.Round(paidAmount, 2)}（立减{itemCurrencyStr}{Math.Round(reward, 2)}）, <br>";
                if (amount - paidAmount > 0)
                {
                    decimal unpaid = Math.Round((amount -reward- paidAmount), 2);
                    Detail += $"UnPaid(待支付)：<b style ='color: red;'>{itemCurrencyStr}{unpaid}</b> <br>";
                }
                Detail += "<br>";
            }
            var AmountInfo = _amountCalculaterUtil.GetOrderPaidInfo(bookings, bookings[0].PayCurrency, bookings[0].ShopId ?? 11, user, country);
            string PaidAmountStr = "0.00";
            if (!string.IsNullOrWhiteSpace(bookings[0].PaymentId))
            {
                DbPaymentInfo dbPaymentInfo = await _paymentRepository.GetOneAsync(a => a.Id == bookings[0].PaymentId);

                var currencySymbol = country.FirstOrDefault(a => a.Currency.ToLower() == dbPaymentInfo.Currency.ToLower()).CurrencySymbol;
                PaidAmountStr = currencySymbol + Math.Round(dbPaymentInfo.Amount, 2);
            }
            string UnpaidAmountStr = AmountInfo.UnPaidAmountText;
            string AmountStr = AmountInfo.AmountText;

            senderParams.PaidAmount = PaidAmountStr;
            senderParams.UnPaidAmount = UnpaidAmountStr;
            senderParams.Amount = AmountStr;
            senderParams.Details = Detail;
            senderParams.ShopSettings = shopInfo.ShopSettings;

            await Send(senderParams);
            //SendToTalCustomer(shopInfo, user, senderParams.Subject, wwwPath, senderParams.TemplateName, AmountStr, PaidAmountStr, UnpaidAmountStr, detailstr);
            return true;
        }
        private void SaveMsgPush(DbBooking item, string mealTime)
        {
            _msgPusherServiceHandler.AddMsg(new Domain.Common.PushMsgModel()
            {
                Id = Guid.NewGuid().ToString(),
                MsgType = MsgTypeEnum.Order,
                SendTime = DateTime.UtcNow,
                Created = DateTime.UtcNow,
                Title = "下单成功通知",
                Message = $"{item.RestaurantName} {mealTime}",
                MessageReference = item.BookingRef,
                Receiver = item.Creater,
                Sender = "GroupMeals",
                ShopId = item.ShopId
            });
        }
        public async Task EmailCustomer(DbBooking item, DbShop shopInfo, string tempName, string wwwPath, string subject)
        {
            string htmlTemp = EmailTemplateUtil.ReadTemplate(wwwPath, tempName);
            string Detail = "";
            string currencyStr = item.PayCurrency;
            string selectDateTimeStr = "";

            decimal amount = item?.AmountInfos.Sum(x => x.Amount) ?? 0;
            decimal paidAmount = item?.AmountInfos.Sum(x => x.PaidAmount) ?? 0;
            decimal reward=item?.AmountInfos.Sum(x=>x.Reward) ?? 0;

            selectDateTimeStr = item.SelectDateTime.Value.GetLocaTimeByIANACode(item.RestaurantTimeZone).ToString("yyyy-MM-dd HH:mm:ss");
            foreach (var course in item?.Courses)
            {
                Detail += $"{course.MenuItemName}({course.Price}) * {course.Qty}  人  {currencyStr}{paidAmount}/{amount}<br>";
            }

            Detail += $"Amount(金额)：<b>{currencyStr}{amount}</b> Paid(已付)：<b>{currencyStr}{paidAmount}</b> UnPaid(待支付)：<b style=\"color: red;\">{currencyStr}{amount - reward - paidAmount}</b>";
            var detailstr = new HtmlString(Detail);
            var emailHtml = "";
            try
            {
                emailHtml = await _contentBuilder.BuildRazorContent(new { selectDateTimeStr, bookingDetail = item, Detail = detailstr }, htmlTemp);
            }
            catch (Exception ex)
            {
                _logger.LogError("----------------emailHtml---error" + ex.Message);
            }
            if (string.IsNullOrWhiteSpace(emailHtml))
            {
                return;
            }
            try
            {
                _emailUtil.SendEmail(shopInfo.ShopSettings, shopInfo.Email, item.ContactEmail, subject, emailHtml);

            }
            catch (Exception ex)
            {
                _logger.LogError($"EmailCustomer Email Customer Error {ex.Message} -{ex.StackTrace} ");
            }
        }
        async Task<string> ModifyOldValue(DbBooking booking, string filedName)
        {
            string oldValue = "";
            if (booking != null)
            {
                var operations = await _operationServiceHandler.GetOpearations(booking.Id);
                foreach (var item in operations)
                {
                    if (item.ModifyType == 4)//&&(DateTime.UtcNow-item.UpdateTime).TotalSeconds<10)
                    {
                        foreach (var filed in item?.ModifyInfos)
                        {
                            if (filed.ModifyField == filedName)
                            {
                                oldValue = filed.oldValue;
                            }
                        }
                    }
                }
            }
            return oldValue;

        }
        public async Task<bool> SendModifiedEmail(DbBooking booking, DbShop shopInfo, string tempName, string wwwPath, string subject)
        {
            string htmlTemp = EmailTemplateUtil.ReadTemplate(wwwPath, tempName);
            string Detail = "";

            decimal amount = 0;
            decimal paidAmount = 0; //item.AmountInfos.Sum(x => x.PaidAmount);

            paidAmount = booking.AmountInfos.Sum(x => x.PaidAmount);
            var reward=booking.AmountInfos.Sum(x=>x.Reward);
            amount = booking.AmountInfos.Sum(x => x.Amount);

            paidAmount = Math.Round(paidAmount, 2);
            amount = Math.Round(amount, 2);
            Detail = "";
            string selectDateTimeStr = booking.SelectDateTime.Value.GetLocaTimeByIANACode(booking.RestaurantTimeZone).ToString("yyyy-MM-dd HH:mm");
            string oldtime = await ModifyOldValue(booking, "SelectDateTime");
            string oldDateTime = "";
            if (!string.IsNullOrWhiteSpace(oldtime))
            {
                DateTime selectTime = DateTime.MinValue;
                DateTime.TryParse(oldtime, out selectTime);
                if (selectTime != DateTime.MinValue)
                {
                    oldDateTime = selectTime.GetLocaTimeByIANACode(booking.RestaurantTimeZone).ToString("yyyy-MM-dd HH:mm");
                }
            }
            if (tempName == EmailConfigs.Instance.Emails[EmailTypeEnum.MealModified].TemplateName)
            {
                Detail += AppendRestaurantInfo(booking);
            }

            if (tempName != EmailConfigs.Instance.Emails[EmailTypeEnum.NewMealRestaurant].TemplateName)
            {
                Detail += AppendCustomerInfo(booking);
            }

            Detail += $"用餐时间：{oldDateTime.ToDelFormat()}{selectDateTimeStr}<br> ";
            List<string> names = new List<string>();
            foreach (var course in booking.Courses)
            {
                int qty = course.Qty + course.ChildrenQty;
                Detail += $"{course.MenuItemName}({course.Price}) * {qty} 人 ";
            }
            string itemCurrencyStr = booking.Currency == "UK" ? "£" : "€";
            //_twilioUtil.sendSMS(booking.RestaurantPhone, "You got a new order. Please see details in groupmeals.com");
            Detail += $"<br> Amount(金额)：<b>{itemCurrencyStr}{booking.AmountInfos.Sum(x => x.Amount)}</b>, <br> Paid(已付)：<b>{itemCurrencyStr}{paidAmount}</b>,<br>";
            if (amount - paidAmount > 0)
                Detail += $" UnPaid(待支付)：<b style=\"color: red;\">{itemCurrencyStr}{amount- reward - paidAmount}</b>";
            var detailstr = new HtmlString(Detail);
            Task.Run(async () =>
            {
                string emailHtml = "";
                try
                {
                    string memo = await ModifyOldValue(booking, "Memo");
                    string memoStr = memo.ToDelFormat() + booking.Courses[0].Memo;
                    emailHtml = await _contentBuilder.BuildRazorContent(new { selectDateTimeStr, booking, bookingDetail = booking, AmountStr = amount, PaidAmountStr = paidAmount, UnpaidAmountStr = amount - reward - paidAmount, Detail = detailstr, Memo = memo.ToDelFormat() + booking.Courses[0].Memo }, htmlTemp);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"------EmailBoss--------{tempName}-{subject}-emailHtml---error" + ex.Message);
                }
                try
                {
                    _logger.LogInfo($"_emailUtil.SendEmailto:{booking.RestaurantEmail}" + booking.BookingRef);
                    await _emailUtil.SendEmail(shopInfo.ShopSettings, shopInfo.Email, booking.RestaurantEmail, subject, emailHtml, "sales.ie@groupmeals.com");

                    _logger.LogInfo("_emailUtil.SendEmailend:" + booking.BookingRef);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"EmailBossError {ex.Message} -{ex.StackTrace} ");
                }
            });
            return true;
        }
        public async Task SendCancelEmail(DbShop shopInfo, DbBooking booking, string webPath, string tempName, string subject, params string[] ccEmail)
        {
            var country = await _coutryHandler.GetCountry(booking.ShopId ?? 11);
            var con = country.FirstOrDefault(a => a.Name == booking.RestaurantCountry);
            if (con == null) return;
            string currencyStr = country.FirstOrDefault(a => a.Currency == booking.PayCurrency).CurrencySymbol;
            decimal exRate = (decimal)(con.ExchangeRate);
            decimal amount = 0;
            decimal paidAmount = 0; booking.AmountInfos.Sum(x => x.PaidAmount);

            paidAmount = booking.AmountInfos.Sum(x => x.PaidAmount);
            var reward = booking.AmountInfos.Sum(x => x.Reward);

            amount = _amountCalculaterUtil.getItemAmount(booking.ConvertToAmount());

            paidAmount = Math.Round(paidAmount, 2);
            amount = Math.Round(amount, 2);

            string selectDateTimeStr = booking.SelectDateTime.Value.GetLocaTimeByIANACode(booking.RestaurantTimeZone).ToString("yyyy-MM-dd HH:mm:ss");
            string Detail = "";
            foreach (var course in booking.Courses)
            {
                Detail += $"{course.MenuItemName}({course.Price}) * {course.Qty} 人 {currencyStr}{paidAmount}/{amount}<br>";
            }
            //_twilioUtil.sendSMS(detail.RestaurantPhone, "You got a new order. Please see details in groupmeals.com");
            Detail += $"Amount(金额)：<b>{currencyStr}{amount}</b>, Paid(已付)：<b>{currencyStr}{paidAmount}</b>, UnPaid(待支付)：<b style=\"color: red;\">{currencyStr}{amount - reward - paidAmount}</b>";
            var detailstr = new HtmlString(Detail);
            var emailParams = EmailConfigs.Instance.Emails[EmailTypeEnum.MealCancelled];
            string htmlTemp = EmailTemplateUtil.ReadTemplate(webPath, tempName);
            var emailHtml = "";
            try
            {
                emailHtml = await _contentBuilder.BuildRazorContent(new { booking, bookingDetail = booking, selectDateTimeStr, Detail = detailstr, Memo = booking.Courses[0].Memo }, htmlTemp);
            }
            catch (Exception ex)
            {
                _logger.LogError($"SendCancelEmail.emailHtml.genrate {ex.Message} -{ex.StackTrace} ");
            }
            try
            {
                _emailUtil.SendEmail(shopInfo.ShopSettings, shopInfo.Email, booking.RestaurantEmail, subject, emailHtml, ccEmail);
                _emailUtil.SendEmail(shopInfo.ShopSettings, shopInfo.Email, booking.ContactEmail, subject, emailHtml, ccEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError($"SendCancelEmail.send {ex.Message} -{ex.StackTrace} ");
            }

        }
        private async Task<string> AppendRestaurantInfo(DbBooking booking, int isShortInfo = 0)
        {
            string detailStr = "";
            string oldValue = await ModifyOldValue(booking, "RestaurantName");
            detailStr += oldValue.ToDelFormat() + booking.RestaurantName + " <br> ";
            oldValue = await ModifyOldValue(booking, "RestaurantAddress");
            detailStr += oldValue.ToDelFormat() + booking.RestaurantAddress + " <br> ";
            if (isShortInfo == 1)
                return detailStr;
            string oldphone = await ModifyOldValue(booking, "RestaurantPhone");
            string oldEmail = await ModifyOldValue(booking, "RestaurantEmail");
            detailStr += oldphone.ToDelFormat() + booking.RestaurantPhone + "  " + oldEmail.ToDelFormat() + booking.RestaurantEmail + " <br> ";
            oldValue = await ModifyOldValue(booking, "RestaurantWechat");
            detailStr += "微信: " + oldValue.ToDelFormat() + booking.RestaurantWechat + " <br> ";
            oldValue = await ModifyOldValue(booking, "EmergencyPhone");
            detailStr += "紧急: " + oldValue.ToDelFormat() + booking.EmergencyPhone + " <br> ";
            return detailStr;
        }
        private async Task<string> AppendCustomerInfo(DbBooking booking, int isShortInfo = 0)
        {
            string detailStr = "";
            string oldValue = await ModifyOldValue(booking, "GroupRef");
            detailStr += "团号: " + oldValue.ToDelFormat() + booking.GroupRef + " <br> ";
            string oldName = await ModifyOldValue(booking, "ContactName");
            string oldPhone = await ModifyOldValue(booking, "ContactPhone");
            detailStr += "联系人: " + oldName.ToDelFormat() + booking.ContactName + " " + oldPhone.ToDelFormat() + booking.ContactPhone + " <br> ";
            if (isShortInfo == 1)
                return detailStr;
            oldValue = await ModifyOldValue(booking, "ContactWechat");
            detailStr += "微信: " + oldValue.ToDelFormat() + booking.ContactWechat + " <br> ";
            oldValue = await ModifyOldValue(booking, "ContactInfos");
            detailStr += "更多联系方式: " + oldValue.ToDelFormat() + booking.ContactInfos + "  <br>";
            oldValue = await ModifyOldValue(booking, "Memo");
            detailStr += "备注: " + oldValue.ToDelFormat() + booking.Memo + " <br> <br> ";
            return detailStr;
        }
    }
    public class EmailData
    {
        public string BookingRef { get; set; }
        public string BookingId { get; set; }
        public string PaidAmount { get; set; }
        public string UnPaidAmount { get; set; }
        public string Amount { get; set; }
        public string RestaurantInfo { get; set; }
        public string CustomerInfo { get; set; }
        public string MealTime { get; set; }
        public string Details { get; set; }
        public string Remark { get; set; }
        public string ReceiverEmail { get; set; }
    }
}
