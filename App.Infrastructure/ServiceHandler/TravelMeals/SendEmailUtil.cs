using App.Domain.Common.Email;
using App.Domain.Common.Shop;
using App.Domain.Enum;
using App.Domain.TravelMeals;
using App.Infrastructure.Builders.Common;
using App.Infrastructure.Exceptions;
using App.Infrastructure.Extensions;
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
        void EmailBoss(TrDbRestaurantBooking booking, DbShop shopInfo, string tempName, string wwwPath, string subject);
        //Task EmailSupport(TrDbRestaurantBooking booking, DbShop shopInfo, string tempName, string wwwPath, string subject, ITwilioUtil _twilioUtil, IContentBuilder _contentBuilder, decimal exRate, ILogManager _logger);
        void EmailCustomerTotal(TrDbRestaurantBooking booking, DbShop shopInfo, string tempName, string wwwPath, string subject);

        Task EmailCustomer(TrDbRestaurantBooking booking, BookingDetail item, DbShop shopInfo, string tempName, string wwwPath, string subject);
        void SendModifiedEmail(TrDbRestaurantBooking booking, DbShop shopInfo, string tempName, string wwwPath, string subject);
        Task SendCancelEmail(DbShop shopInfo, TrDbRestaurantBooking booking, BookingDetail detail, string webPath, string tempName, string subject, params string[] ccEmail);
    }
    public class SendEmailUtil : ISendEmailUtil
    {

        private readonly IEmailUtil _emailUtil; ILogManager _logger;
        IContentBuilder _contentBuilder; private readonly IDateTimeUtil _dateTimeUtil;
        public SendEmailUtil(IEmailUtil emailUtil, ILogManager logger, IDateTimeUtil dateTimeUtil, IContentBuilder contentBuilder)
        {
            _emailUtil = emailUtil;
            _logger = logger;
            _contentBuilder = contentBuilder;
            _dateTimeUtil = dateTimeUtil;

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

        public void EmailBoss(TrDbRestaurantBooking booking, DbShop shopInfo, string tempName, string wwwPath, string subject)
        {
            string htmlTemp = EmailTemplateUtil.ReadTemplate(wwwPath, tempName);
            string Detail = "";
            foreach (var item in booking.Details)
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
                        emailHtml = await _contentBuilder.BuildRazorContent(new { selectDateTimeStr, booking, bookingDetail = item, AmountStr = amount, PaidAmountStr = paidAmount, UnpaidAmountStr = amount - paidAmount, Detail = detailstr, Memo = item.Courses[0].Memo }, htmlTemp);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"------EmailBoss--------{tempName}-{subject}-emailHtml---error" + ex.Message);
                    }
                    try
                    {
                        _logger.LogInfo($"_emailUtil.SendEmailto:{item.RestaurantEmail}" + booking.BookingRef);
                        await _emailUtil.SendEmail(shopInfo.ShopSettings, shopInfo.Email, item.RestaurantEmail, subject, emailHtml, "sales.ie@groupmeals.com");

                        _logger.LogInfo("_emailUtil.SendEmailend:" + booking.BookingRef);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"EmailBossError {ex.Message} -{ex.StackTrace} ");
                    }
                });
            }
        }
        private string AppendRestaurantInfo(BookingDetail item, TrDbRestaurantBooking booking = null)
        {
            string detailStr = "";
            string oldValue = ModifyOldValue(booking, item.Id, "RestaurantName");
            detailStr += oldValue.ToDelFormat() + item.RestaurantName + " <br> ";
            oldValue = ModifyOldValue(booking, item.Id, "RestaurantAddress");
            detailStr += oldValue.ToDelFormat() + item.RestaurantAddress + " <br> ";
            string oldphone = ModifyOldValue(booking, item.Id, "RestaurantPhone");
           string oldEmail = ModifyOldValue(booking, item.Id, "RestaurantEmail");
            detailStr += oldphone.ToDelFormat() + item.RestaurantPhone + "  " + oldEmail.ToDelFormat()+ item.RestaurantEmail + " <br> ";
            oldValue = ModifyOldValue(booking, item.Id, "RestaurantWechat");
            detailStr += "微信: " + oldValue.ToDelFormat() + item.RestaurantWechat + " <br> ";
            oldValue = ModifyOldValue(booking, item.Id, "EmergencyPhone");
            detailStr += "紧急: " + oldValue.ToDelFormat() + item.EmergencyPhone + " <br> ";
            return detailStr;
        }
        private string AppendCustomerInfo(BookingDetail item, TrDbRestaurantBooking booking = null)
        {
            string detailStr = "";
            string oldValue = ModifyOldValue(booking, item.Id, "GroupRef");
            detailStr += "团号: " + oldValue.ToDelFormat() + item.GroupRef + " <br> ";
            string oldName = ModifyOldValue(booking, item.Id, "ContactName");
            string oldPhone = ModifyOldValue(booking, item.Id, "ContactPhone");
            detailStr += "联系人: " + oldName.ToDelFormat()+ item.ContactName + " " + oldPhone.ToDelFormat()+ item.ContactPhone + " <br> ";
             oldValue = ModifyOldValue(booking, item.Id, "ContactWechat");
            detailStr += "微信: " + oldValue.ToDelFormat() + item.ContactWechat + " <br> ";
             oldValue = ModifyOldValue(booking, item.Id, "ContactInfos");
            detailStr += "更多联系方式: " + oldValue.ToDelFormat() + item.ContactInfos + "  <br>";
            oldValue = ModifyOldValue(booking, item.Id, "Memo");
            detailStr += "备注: " + oldValue.ToDelFormat() + item.Memo + " <br> <br> ";
            return detailStr;
        }

        public void EmailCustomerTotal(TrDbRestaurantBooking booking, DbShop shopInfo, string tempName, string wwwPath, string subject)
        {
            string htmlTemp = EmailTemplateUtil.ReadTemplate(wwwPath, tempName);
            string Detail = "";
            decimal totalAmount = 0, totalPaidAmount = 0;
            decimal UKAmount = 0, EUAmount = 0, UKUnPaidAmount = 0, EUUnPaidAmount = 0;
            string currencyStr = booking.PayCurrency == "UK" ? "£" : "€";
            foreach (var item in booking.Details)
            {
                if (item.Status == OrderStatusEnum.Canceled) continue;
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
                    emailHtml = await _contentBuilder.BuildRazorContent(new { booking, bookingDetail = booking.Details[0], AmountStr, PaidAmountStr, UnpaidAmountStr, Detail = detailstr }, htmlTemp);
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
                    await _emailUtil.SendEmail(shopInfo.ShopSettings, shopInfo.Email, booking.CustomerEmail, subject, emailHtml);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"EmailCustomer Email Customer Error {ex.Message} -{ex.StackTrace} ");
                }
            });

        }
        public async Task EmailCustomer(TrDbRestaurantBooking booking, BookingDetail item, DbShop shopInfo, string tempName, string wwwPath, string subject)
        {
            string htmlTemp = EmailTemplateUtil.ReadTemplate(wwwPath, tempName);
            string Detail = "";
            decimal totalAmount = 0, totalPaidAmount = 0;
            string currencyStr = booking?.PayCurrency == "UK" ? "£" : "€";
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
                emailHtml = await _contentBuilder.BuildRazorContent(new { selectDateTimeStr, booking, bookingDetail = item, Detail = detailstr }, htmlTemp);
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
                _emailUtil.SendEmail(shopInfo.ShopSettings, shopInfo.Email, booking.CustomerEmail, subject, emailHtml);

            }
            catch (Exception ex)
            {
                _logger.LogError($"EmailCustomer Email Customer Error {ex.Message} -{ex.StackTrace} ");
            }
        }
        string ModifyOldValue(TrDbRestaurantBooking booking, string dtlId, string filedName)
        {
            string oldValue = "";
            if (booking != null)
            {
                foreach (var item in booking?.Operations)
                {
                    if (item.ModifyType == 4)//&&(DateTime.UtcNow-item.UpdateTime).TotalSeconds<10)
                    {
                        foreach (var filed in item?.ModifyInfos)
                        {
                            if (filed.ModifyField == filedName && filed.ModifyLocation.Contains(dtlId))
                            {
                                oldValue = filed.oldValue;
                            }
                        }
                    }
                }
            }
            return oldValue;

        }
        public void SendModifiedEmail(TrDbRestaurantBooking booking, DbShop shopInfo, string tempName, string wwwPath, string subject)
        {
            string htmlTemp = EmailTemplateUtil.ReadTemplate(wwwPath, tempName);
            string Detail = "";
            foreach (var item in booking.Details)
            {
                decimal amount = 0;
                decimal paidAmount = 0; //item.AmountInfos.Sum(x => x.PaidAmount);

                paidAmount = item.AmountInfos.Sum(x => x.PaidAmount);

                amount = item.AmountInfos.Sum(x => x.Amount);

                paidAmount = Math.Round(paidAmount, 2);
                amount = Math.Round(amount, 2);
                Detail = "";
                string selectDateTimeStr = item.SelectDateTime.Value.GetLocaTimeByIANACode(_dateTimeUtil.GetIANACode(item.RestaurantCountry)).ToString("yyyy-MM-dd HH:mm");
                string oldtime = ModifyOldValue(booking, item.Id, "SelectDateTime");
                string oldDateTime = "";
                if (!string.IsNullOrWhiteSpace(oldtime))
                {
                    DateTime selectTime = DateTime.MinValue;
                    DateTime.TryParse(oldtime, out selectTime);
                    if (selectTime != DateTime.MinValue)
                    {
                        oldDateTime = selectTime.GetLocaTimeByIANACode(_dateTimeUtil.GetIANACode(item.RestaurantCountry)).ToString("yyyy-MM-dd HH:mm");
                    }
                }
                if (tempName == EmailConfigs.Instance.Emails[EmailTypeEnum.MealModified_V2].TemplateName)
                {
                    Detail += AppendRestaurantInfo(item, booking);
                }

                if (tempName != EmailConfigs.Instance.Emails[EmailTypeEnum.NewMealRestaurant].TemplateName)
                {
                    Detail += AppendCustomerInfo(item, booking);
                }

                Detail += $"用餐时间：{oldDateTime.ToDelFormat()}{selectDateTimeStr}<br> ";
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
                        string memo = ModifyOldValue(booking, item.Id, "Memo");
                        string memoStr = memo.ToDelFormat() + item.Courses[0].Memo;
                        emailHtml = await _contentBuilder.BuildRazorContent(new { selectDateTimeStr, booking, bookingDetail = item, AmountStr = amount, PaidAmountStr = paidAmount, UnpaidAmountStr = amount - paidAmount, Detail = detailstr, Memo =memo.ToDelFormat()+ item.Courses[0].Memo }, htmlTemp);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"------EmailBoss--------{tempName}-{subject}-emailHtml---error" + ex.Message);
                    }
                    try
                    {
                        _logger.LogInfo($"_emailUtil.SendEmailto:{item.RestaurantEmail}" + booking.BookingRef);
                        await _emailUtil.SendEmail(shopInfo.ShopSettings, shopInfo.Email, item.RestaurantEmail, subject, emailHtml, "sales.ie@groupmeals.com");

                        _logger.LogInfo("_emailUtil.SendEmailend:" + booking.BookingRef);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"EmailBossError {ex.Message} -{ex.StackTrace} ");
                    }
                });
            }
        }
        public async Task SendCancelEmail(DbShop shopInfo, TrDbRestaurantBooking booking, BookingDetail detail, string webPath, string tempName, string subject, params string[] ccEmail)
        {
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
