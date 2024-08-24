using App.Domain.Common.Email;
using App.Domain.Common.Shop;
using App.Domain.Enum;
using App.Domain.TravelMeals;
using App.Infrastructure.Builders.Common;
using App.Infrastructure.Exceptions;
using App.Infrastructure.Extensions;
using App.Infrastructure.ServiceHandler.Common;
using App.Infrastructure.ServiceHandler.Tour;
using App.Infrastructure.Utility.Common;
using Hangfire;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http.Metadata;
using Quartz.Impl.Triggers;
using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace App.Infrastructure.ServiceHandler.TravelMeals
{

    public interface ISendEmailUtil
    {
        Task EmailVerifyCode(string email, string code, DbShop shopInfo, string tempName, string wwwPath, string subject, string title, string titleCN);
        void EmailBoss(List<DbBooking> bookings, DbShop shopInfo, string tempName, string wwwPath, string subject);
        //Task EmailSupport(TrDbRestaurantBooking booking, DbShop shopInfo, string tempName, string wwwPath, string subject, ITwilioUtil _twilioUtil, IContentBuilder _contentBuilder, decimal exRate, ILogManager _logger);
        void EmailCustomerTotal(List<DbBooking> bookings, DbShop shopInfo, string tempName, string wwwPath, string subject);

        Task EmailCustomer(DbBooking booking, DbShop shopInfo, string tempName, string wwwPath, string subject);
        void SendModifiedEmail(DbBooking booking, DbShop shopInfo, string tempName, string wwwPath, string subject);
        Task SendCancelEmail(DbShop shopInfo, TrDbRestaurantBooking booking, DbBooking detail, string webPath, string tempName, string subject, params string[] ccEmail);
    }
    public class SendEmailUtil : ISendEmailUtil
    {

        private readonly IEmailUtil _emailUtil;
        ILogManager _logger;
        IContentBuilder _contentBuilder;
        ICountryServiceHandler _coutryHandler;
        private readonly IDateTimeUtil _dateTimeUtil;
        IAmountCalculaterUtil _amountCalculaterUtil;
        IOperationServiceHandler _operationServiceHandler;

        public SendEmailUtil(IEmailUtil emailUtil, IAmountCalculaterUtil amountCalculaterUtil, ILogManager logger, IDateTimeUtil dateTimeUtil, ICountryServiceHandler coutryHandler,
           IOperationServiceHandler operationServiceHandler, IContentBuilder contentBuilder)
        {
            _emailUtil = emailUtil;
            _logger = logger;
            _contentBuilder = contentBuilder;
            _dateTimeUtil = dateTimeUtil;
            _coutryHandler = coutryHandler;
            _amountCalculaterUtil = amountCalculaterUtil;
            _operationServiceHandler = operationServiceHandler;
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

        public void EmailBoss(List<DbBooking> bookings, DbShop shopInfo, string tempName, string wwwPath, string subject)
        {
            string htmlTemp = EmailTemplateUtil.ReadTemplate(wwwPath, tempName);
            string Detail = "";
            foreach (var item in bookings)
            {
                decimal amount = 0;
                decimal paidAmount = 0; //item.AmountInfos.Sum(x => x.PaidAmount);

                paidAmount = item.AmountInfos.Sum(x => x.PaidAmount);

                amount = item.AmountInfos.Sum(x => x.Amount);

                paidAmount = Math.Round(paidAmount, 2);
                amount = Math.Round(amount, 2);
                Detail = "";
                string selectDateTimeStr = item.SelectDateTime.Value.GetLocaTimeByIANACode(_dateTimeUtil.GetIANACode(item.RestaurantCountry)).ToString("yyyy-MM-dd HH:mm");
                if (tempName == EmailConfigs.Instance.Emails[EmailTypeEnum.MealModified].TemplateName)
                {
                    Detail += AppendRestaurantInfo(item);
                    Detail += "<br>" + selectDateTimeStr + " <br><br> ";
                }

                if (tempName != EmailConfigs.Instance.Emails[EmailTypeEnum.NewMealRestaurant].TemplateName)
                {
                    Detail += AppendCustomerInfo(item);
                }
                List<string> names = new List<string>();
                foreach (var course in item.Courses)
                {
                    int qty = course.Qty + course.ChildrenQty;
                    Detail += $"{course.MenuItemName} * {qty} 人 ";
                }
                string itemCurrencyStr = item.Currency == "UK" ? "£" : "€";
                //_twilioUtil.sendSMS(item.RestaurantPhone, "You got a new order. Please see details in groupmeals.com");
                Detail += $"<br> Amount(金额)：<b>{itemCurrencyStr}{item.AmountInfos.Sum(x => x.Amount)}</b>, <br> Paid(已付)：<b>{itemCurrencyStr}{paidAmount}</b>,<br>";
                if (amount - paidAmount > 0)
                    Detail += $" UnPaid(待支付)：<b style=\"color: red;\">{itemCurrencyStr}{amount - paidAmount}</b>";
                var detailstr = new HtmlString(Detail);
                Task.Run(async () =>
                {
                    string emailHtml = "";
                    try
                    {
                        emailHtml = await _contentBuilder.BuildRazorContent(new { selectDateTimeStr, bookingDetail = item, AmountStr = amount, PaidAmountStr = paidAmount, UnpaidAmountStr = amount - paidAmount, Detail = detailstr, Memo = item.Courses[0].Memo }, htmlTemp);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"------EmailBoss--------{tempName}-{subject}-emailHtml---error" + ex.Message);
                    }
                    try
                    {
                        _logger.LogInfo($"_emailUtil.SendEmailto:{item.RestaurantEmail}" + item.BookingRef);
                        await _emailUtil.SendEmail(shopInfo.ShopSettings, shopInfo.Email, item.RestaurantEmail, subject, emailHtml, "sales.ie@groupmeals.com");

                        _logger.LogInfo("_emailUtil.SendEmailend:" + item.BookingRef);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"EmailBossError {ex.Message} -{ex.StackTrace} ");
                    }
                });
            }
        }
        private async Task<string> AppendRestaurantInfo(DbBooking booking)
        {
            string detailStr = "";
            string oldValue = await ModifyOldValue(booking, "RestaurantName");
            detailStr += oldValue.ToDelFormat() + booking.RestaurantName + " <br> ";
            oldValue = await ModifyOldValue(booking, "RestaurantAddress");
            detailStr += oldValue.ToDelFormat() + booking.RestaurantAddress + " <br> ";
            string oldphone = await ModifyOldValue(booking, "RestaurantPhone");
            string oldEmail = await ModifyOldValue(booking, "RestaurantEmail");
            detailStr += oldphone.ToDelFormat() + booking.RestaurantPhone + "  " + oldEmail.ToDelFormat() + booking.RestaurantEmail + " <br> ";
            oldValue = await ModifyOldValue(booking, "RestaurantWechat");
            detailStr += "微信: " + oldValue.ToDelFormat() + booking.RestaurantWechat + " <br> ";
            oldValue = await ModifyOldValue(booking, "EmergencyPhone");
            detailStr += "紧急: " + oldValue.ToDelFormat() + booking.EmergencyPhone + " <br> ";
            return detailStr;
        }
        private async Task<string> AppendCustomerInfo(DbBooking booking)
        {
            string detailStr = "";
            string oldValue = await ModifyOldValue(booking, "GroupRef");
            detailStr += "团号: " + oldValue.ToDelFormat() + booking.GroupRef + " <br> ";
            string oldName = await ModifyOldValue(booking, "ContactName");
            string oldPhone = await ModifyOldValue(booking, "ContactPhone");
            detailStr += "联系人: " + oldName.ToDelFormat() + booking.ContactName + " " + oldPhone.ToDelFormat() + booking.ContactPhone + " <br> ";
            oldValue = await ModifyOldValue(booking, "ContactWechat");
            detailStr += "微信: " + oldValue.ToDelFormat() + booking.ContactWechat + " <br> ";
            oldValue = await ModifyOldValue(booking, "ContactInfos");
            detailStr += "更多联系方式: " + oldValue.ToDelFormat() + booking.ContactInfos + "  <br>";
            oldValue = await ModifyOldValue(booking, "Memo");
            detailStr += "备注: " + oldValue.ToDelFormat() + booking.Memo + " <br> <br> ";
            return detailStr;
        }

        public void EmailCustomerTotal(List<DbBooking> bookings, DbShop shopInfo, string tempName, string wwwPath, string subject)
        {
            string htmlTemp = EmailTemplateUtil.ReadTemplate(wwwPath, tempName);
            string Detail = "";
            decimal totalAmount = 0, totalPaidAmount = 0;
            decimal UKAmount = 0, EUAmount = 0, UKUnPaidAmount = 0, EUUnPaidAmount = 0;
            string currencyStr = "";
            string reciveEmail = "";//TODO
            foreach (var item in bookings)
            {
                if (item.Status == OrderStatusEnum.Canceled) continue;
                reciveEmail = item.Customer.ContactEmail;
                currencyStr = item.PayCurrency;
                if (tempName != EmailConfigs.Instance.Emails[EmailTypeEnum.NewMealCustomer].TemplateName)
                    Detail += AppendRestaurantInfo(item);
                string selectDateTimeStr = item.SelectDateTime.Value.GetLocaTimeByIANACode(_dateTimeUtil.GetIANACode(item.RestaurantCountry)).ToString("yyyy-MM-dd HH:mm");
                Detail += selectDateTimeStr + " <br><br> ";

                Detail += AppendCustomerInfo(item);
                decimal amount = item.AmountInfos.Sum(x => x.Amount);
                decimal paidAmount = item.AmountInfos.Sum(x => x.PaidAmount);


                totalPaidAmount += paidAmount;
                if (item.Currency == "UK")
                {
                    UKAmount += amount;
                    UKUnPaidAmount += amount - paidAmount;
                }
                else
                {
                    EUAmount += amount;
                    EUUnPaidAmount += amount - paidAmount;
                }

                string itemCurrencyStr = item.Currency == "UK" ? "£" : "€";
                List<string> names = new List<string>();
                foreach (var course in item.Courses)
                {
                    int qty = course.Qty + course.ChildrenQty;
                    Detail += $"{course.MenuItemName} * {qty} 人 ";
                }
                Detail += $"<br> Amount(金额)：{itemCurrencyStr}{Math.Round(amount, 2)}，    Paid(已付){itemCurrencyStr}{Math.Round(paidAmount, 2)}, <br>";
                if (amount - paidAmount > 0)
                    Detail += $"UnPaid(待支付)：<b style ='color: red;'>{itemCurrencyStr}{amount - paidAmount}</b> <br>";

                Detail += "<br>";
            }
            string PaidAmountStr = currencyStr + Math.Round(totalPaidAmount, 2);
            string UnpaidAmountStr = "";
            string AmountStr = "";

            if (UKAmount > 0 && EUAmount > 0)
                AmountStr = $"€{Math.Round(EUAmount, 2)} + £{Math.Round(UKAmount, 2)}";
            else
                AmountStr = UKAmount > 0 ? $"£{Math.Round(UKAmount, 2)}" : $"€{Math.Round(EUAmount, 2)}";

            if (UKUnPaidAmount > 0 && EUUnPaidAmount > 0)
                UnpaidAmountStr = $"€{Math.Round(EUUnPaidAmount, 2)} + £{Math.Round(UKUnPaidAmount, 2)}";
            else
                UnpaidAmountStr = UKUnPaidAmount > 0 ? $"£{Math.Round(UKUnPaidAmount, 2)}" : $"€{Math.Round(EUUnPaidAmount, 2)}";

            //Detail += $"Amount(金额)：<b>{currencyStr}{totalAmount}</b> Paid(已付)：<b>{currencyStr}{totalPaidAmount}</b> UnPaid(待支付)：<b style=\"color: red;\">{currencyStr}{totalAmount - totalPaidAmount}</b>";
            var detailstr = new HtmlString(Detail);
            var emailHtml = "";
            Task.Run(async () =>
            {
                try
                {
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
                    await _emailUtil.SendEmail(shopInfo.ShopSettings, shopInfo.Email, reciveEmail, subject, emailHtml);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"EmailCustomer Email Customer Error {ex.Message} -{ex.StackTrace} ");
                }
            });

        }
        public async Task EmailCustomer(DbBooking item, DbShop shopInfo, string tempName, string wwwPath, string subject)
        {
            string htmlTemp = EmailTemplateUtil.ReadTemplate(wwwPath, tempName);
            string Detail = "";
            decimal totalAmount = 0, totalPaidAmount = 0;
            string currencyStr = item.PayCurrency;
            string selectDateTimeStr = "";

            decimal amount = item?.AmountInfos.Sum(x => x.Amount) ?? 0;
            totalAmount += amount;
            decimal paidAmount = item?.AmountInfos.Sum(x => x.PaidAmount) ?? 0;
            totalPaidAmount += paidAmount;
            selectDateTimeStr = item.SelectDateTime.Value.GetLocaTimeByIANACode(_dateTimeUtil.GetIANACode(item.RestaurantCountry)).ToString("yyyy-MM-dd HH:mm:ss");
            foreach (var course in item?.Courses)
            {
                Detail += $"{course.MenuItemName} * {course.Qty}  人  {currencyStr}{paidAmount}/{amount}<br>";
            }

            Detail += $"Amount(金额)：<b>{currencyStr}{totalAmount}</b> Paid(已付)：<b>{currencyStr}{totalPaidAmount}</b> UnPaid(待支付)：<b style=\"color: red;\">{currencyStr}{totalAmount - totalPaidAmount}</b>";
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
        public async void SendModifiedEmail(DbBooking booking, DbShop shopInfo, string tempName, string wwwPath, string subject)
        {
            string htmlTemp = EmailTemplateUtil.ReadTemplate(wwwPath, tempName);
            string Detail = "";

            decimal amount = 0;
            decimal paidAmount = 0; //item.AmountInfos.Sum(x => x.PaidAmount);

            paidAmount = booking.AmountInfos.Sum(x => x.PaidAmount);

            amount = booking.AmountInfos.Sum(x => x.Amount);

            paidAmount = Math.Round(paidAmount, 2);
            amount = Math.Round(amount, 2);
            Detail = "";
            string selectDateTimeStr = booking.SelectDateTime.Value.GetLocaTimeByIANACode(_dateTimeUtil.GetIANACode(booking.RestaurantCountry)).ToString("yyyy-MM-dd HH:mm");
            string oldtime = await ModifyOldValue(booking, "SelectDateTime");
            string oldDateTime = "";
            if (!string.IsNullOrWhiteSpace(oldtime))
            {
                DateTime selectTime = DateTime.MinValue;
                DateTime.TryParse(oldtime, out selectTime);
                if (selectTime != DateTime.MinValue)
                {
                    oldDateTime = selectTime.GetLocaTimeByIANACode(_dateTimeUtil.GetIANACode(booking.RestaurantCountry)).ToString("yyyy-MM-dd HH:mm");
                }
            }
            if (tempName == EmailConfigs.Instance.Emails[EmailTypeEnum.MealModified_V2].TemplateName)
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
                Detail += $"{course.MenuItemName} * {qty} 人 ";
            }
            string itemCurrencyStr = booking.Currency == "UK" ? "£" : "€";
            //_twilioUtil.sendSMS(booking.RestaurantPhone, "You got a new order. Please see details in groupmeals.com");
            Detail += $"<br> Amount(金额)：<b>{itemCurrencyStr}{booking.AmountInfos.Sum(x => x.Amount)}</b>, <br> Paid(已付)：<b>{itemCurrencyStr}{paidAmount}</b>,<br>";
            if (amount - paidAmount > 0)
                Detail += $" UnPaid(待支付)：<b style=\"color: red;\">{itemCurrencyStr}{amount - paidAmount}</b>";
            var detailstr = new HtmlString(Detail);
            Task.Run(async () =>
            {
                string emailHtml = "";
                try
                {
                    string memo = await ModifyOldValue(booking, "Memo");
                    string memoStr = memo.ToDelFormat() + booking.Courses[0].Memo;
                    emailHtml = await _contentBuilder.BuildRazorContent(new { selectDateTimeStr, booking, bookingDetail = booking, AmountStr = amount, PaidAmountStr = paidAmount, UnpaidAmountStr = amount - paidAmount, Detail = detailstr, Memo = memo.ToDelFormat() + booking.Courses[0].Memo }, htmlTemp);
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

        }
        public async Task SendCancelEmail(DbShop shopInfo, TrDbRestaurantBooking booking, DbBooking detail, string webPath, string tempName, string subject, params string[] ccEmail)
        {
            var country = await _coutryHandler.GetCountry(booking.ShopId ?? 11);
            var con = country.Countries.FirstOrDefault(a => a.Name == detail.RestaurantCountry);
            if (con == null) return;
            string currencyStr = country.Countries.FirstOrDefault(a => a.Currency == booking.PayCurrency).CurrencySymbol;
            decimal exRate = (decimal)(con.ExchangeRate);
            decimal amount = 0;
            decimal paidAmount = 0; detail.AmountInfos.Sum(x => x.PaidAmount);

            paidAmount = detail.AmountInfos.Sum(x => x.PaidAmount);

            amount = await _amountCalculaterUtil.CalculateAmountByRate(detail, booking.PayCurrency, booking.ShopId ?? 11, country);

            paidAmount = Math.Round(paidAmount, 2);
            amount = Math.Round(amount, 2);

            string selectDateTimeStr = detail.SelectDateTime.Value.GetLocaTimeByIANACode(_dateTimeUtil.GetIANACode(detail.RestaurantCountry)).ToString("yyyy-MM-dd HH:mm:ss");
            string Detail = "";
            foreach (var course in detail.Courses)
            {
                Detail += $"{course.MenuItemName} * {course.Qty} 人 {currencyStr}{paidAmount}/{amount}<br>";
            }
            //_twilioUtil.sendSMS(detail.RestaurantPhone, "You got a new order. Please see details in groupmeals.com");
            Detail += $"Amount(金额)：<b>{currencyStr}{amount}</b>, Paid(已付)：<b>{currencyStr}{paidAmount}</b>, UnPaid(待支付)：<b style=\"color: red;\">{currencyStr}{amount - paidAmount}</b>";
            var detailstr = new HtmlString(Detail);
            var emailParams = EmailConfigs.Instance.Emails[EmailTypeEnum.MealCancelled];
            string htmlTemp = EmailTemplateUtil.ReadTemplate(webPath, tempName);
            var emailHtml = "";
            try
            {
                emailHtml = await _contentBuilder.BuildRazorContent(new { booking, bookingDetail = detail, selectDateTimeStr, Detail = detailstr, Memo = detail.Courses[0].Memo }, htmlTemp);
            }
            catch (Exception ex)
            {
                _logger.LogError($"SendCancelEmail.emailHtml.genrate {ex.Message} -{ex.StackTrace} ");
            }
            try
            {
                _emailUtil.SendEmail(shopInfo.ShopSettings, shopInfo.Email, detail.RestaurantEmail, subject, emailHtml, ccEmail);
                _emailUtil.SendEmail(shopInfo.ShopSettings, shopInfo.Email, booking.CustomerEmail, subject, emailHtml, ccEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError($"SendCancelEmail.send {ex.Message} -{ex.StackTrace} ");
            }

        }
    }
}
