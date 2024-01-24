using App.Domain.Common.Shop;
using App.Domain.TravelMeals;
using App.Infrastructure.Builders.Common;
using App.Infrastructure.ServiceHandler.Tour;
using App.Infrastructure.Utility.Common;
using Hangfire;
using Microsoft.AspNetCore.Html;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Infrastructure.ServiceHandler.TravelMeals
{
   
    public class EmailUtils 
    {
        public static async Task EmailBoss(TrDbRestaurantBooking booking, DbShop shopInfo, string subject,string wwwPath, ITwilioUtil _twilioUtil,IContentBuilder _contentBuilder, ILogManager _logger)
        {
            string htmlTemp = EmailTemplateUtil.ReadTemplate(wwwPath, "new_meals_restaurant");
            string currencyStr = booking.PayCurrency == "UK" ? "￡" : "€";
            string Detail = "";
            foreach (var item in booking.Details)
            {
                decimal amount = item.PaymentInfos.Sum(x => x.Amount);
                decimal paidAmount = item.PaymentInfos.Sum(x => x.PaidAmount);
                Detail = "";
                foreach (var course in item.Courses)
                {
                    Detail += $"{course.MenuItemName} * {course.Qty} 人 {currencyStr}{paidAmount}/{amount}<br>";
                }
                _twilioUtil.sendSMS(item.RestaurantPhone, "You got a new order. Please see details in groupmeals.com");
                Detail += $"Amount(金额)：<b>{currencyStr}{amount}</b> Paid(已付)：<b>{currencyStr}{paidAmount}</b> UnPaid(待支付)：<b style=\"color: red;\">{currencyStr}{amount - paidAmount}</b>";
                var detailstr = new HtmlString(Detail);
                var emailHtml = await _contentBuilder.BuildRazorContent(new { booking = booking, details = item, Detail= detailstr, Memo = item.Courses[0].Memo }, htmlTemp);
                try
                {
                    BackgroundJob.Enqueue<ITourBatchServiceHandler>(s => s.SendEmail(shopInfo.ShopSettings, shopInfo.Email, item.RestaurantEmail, subject,emailHtml));
                }
                catch (Exception ex)
                {
                    _logger.LogError($"EmailBossError {ex.Message} -{ex.StackTrace} ");
                }
            }
        }

        public static async Task EmailSupport(TrDbRestaurantBooking booking, DbShop shopInfo, string subject, string wwwPath, ITwilioUtil _twilioUtil, IContentBuilder _contentBuilder, ILogManager _logger)
        {
            string htmlTemp = EmailTemplateUtil.ReadTemplate(wwwPath, "new_meals_support");
            string currencyStr = booking.PayCurrency == "UK" ? "￡" : "€";
            foreach (var item in booking.Details)
            {
                decimal amount = item.PaymentInfos.Sum(x => x.Amount);
                decimal paidAmount = item.PaymentInfos.Sum(x => x.PaidAmount);
                string Detail = "";
                foreach (var course in item.Courses)
                {
                    Detail += $"{course.MenuItemName} * {course.Qty} 人 {currencyStr}{paidAmount}/{amount}<br>";
                }
                _twilioUtil.sendSMS(booking.Details[0].RestaurantPhone, "You got a new order. Please see details in groupmeals.com");
                Detail += $"Amount(金额)：<b>{currencyStr}{amount}</b> Paid(已付)：<b>{currencyStr}{paidAmount}</b> UnPaid(待支付)：<b style=\"color: red;\">{currencyStr}{amount - paidAmount}</b>";
                var detailstr = new HtmlString(Detail);
                var emailHtml = await _contentBuilder.BuildRazorContent(new { booking = booking, details = item, Detail= detailstr, Memo = item.Courses[0].Memo }, htmlTemp);
                try
                {
                    BackgroundJob.Enqueue<ITourBatchServiceHandler>(s => s.SendEmail(shopInfo.ShopSettings, shopInfo.Email, item.SupporterEmail, subject,emailHtml));
                }
                catch (Exception ex)
                {
                    _logger.LogError($"EmailBossError {ex.Message} -{ex.StackTrace} ");
                }
            }
        }

        public static async Task EmailCustomerTotal(TrDbRestaurantBooking booking, DbShop shopInfo, string tempName, string wwwPath, IContentBuilder _contentBuilder, ILogManager _logger)
        {
            string htmlTemp = EmailTemplateUtil.ReadTemplate(wwwPath, tempName);
            string Detail = "";
            decimal totalAmount = 0, totalPaidAmount = 0;
            string currencyStr = booking.PayCurrency == "UK" ? "￡" : "€";
            foreach (var item in booking.Details)
            {
                Detail += item.RestaurantName + "<br>";
                decimal amount = item.PaymentInfos.Sum(x => x.Amount);
                totalAmount += amount;
                decimal paidAmount = item.PaymentInfos.Sum(x => x.PaidAmount);
                totalPaidAmount += paidAmount;
                foreach (var course in item.Courses)
                {
                    Detail += $"{course.MenuItemName} * {course.Qty}  人<br>  {currencyStr}{paidAmount}/{amount}<br>";
                }
            }
            Detail += $"Amount(金额)：<b>{currencyStr}{totalAmount}</b> Paid(已付)：<b>{currencyStr}{totalPaidAmount}</b> UnPaid(待支付)：<b style=\"color: red;\">{currencyStr}{totalAmount - totalPaidAmount}</b>";
            var detailstr = new HtmlString(Detail);
            var emailHtml = "";
            try
            {
                emailHtml = await _contentBuilder.BuildRazorContent(new { booking, bookingDetail = booking.Details[0], Detail = detailstr }, htmlTemp);
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
                BackgroundJob.Enqueue<ITourBatchServiceHandler>(s => s.SendEmail(shopInfo.ShopSettings, shopInfo.Email, booking.CustomerEmail, $"Thank you for your Booking", emailHtml));

            }
            catch (Exception ex)
            {
                _logger.LogError($"EmailCustomer Email Customer Error {ex.Message} -{ex.StackTrace} ");
            }

        }
            public static async Task EmailCustomer(TrDbRestaurantBooking booking, DbShop shopInfo, string tempName,string wwwPath,IContentBuilder _contentBuilder, ILogManager _logger)
        {
            string htmlTemp = EmailTemplateUtil.ReadTemplate(wwwPath, tempName);
            string Detail = "";
            decimal totalAmount=0, totalPaidAmount=0;
            string currencyStr = booking.PayCurrency == "UK" ? "￡" : "€";
            foreach (var item in booking.Details)
            {
                Detail += item.RestaurantName + "       ";
                decimal amount=item.PaymentInfos.Sum(x => x.Amount);
                totalAmount+= amount;
                decimal paidAmount=item.PaymentInfos.Sum(x=>x.PaidAmount);
                totalPaidAmount+= paidAmount;
                foreach (var course in item.Courses)
                {
                    Detail += $"{course.MenuItemName} * {course.Qty}  人  {currencyStr}{paidAmount}/{amount}<br>";
                }
            }
            Detail += $"Amount(金额)：<b>{currencyStr}{totalAmount}</b> Paid(已付)：<b>{currencyStr}{totalPaidAmount}</b> UnPaid(待支付)：<b style=\"color: red;\">{currencyStr}{totalAmount- totalPaidAmount}</b>";
            var detailstr = new HtmlString(Detail);
            var emailHtml = "";
            try
            {
                emailHtml = await _contentBuilder.BuildRazorContent(new {  booking,bookingDetail = booking.Details[0], Detail= detailstr }, htmlTemp);
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
                BackgroundJob.Enqueue<ITourBatchServiceHandler>(s => s.SendEmail(shopInfo.ShopSettings, shopInfo.Email, booking.CustomerEmail, $"Thank you for your Booking",emailHtml));

            }
            catch (Exception ex)
            {
                _logger.LogError($"EmailCustomer Email Customer Error {ex.Message} -{ex.StackTrace} ");
            }
        }
    }
}
