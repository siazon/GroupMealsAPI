using App.Domain.Common.Shop;
using App.Domain.TravelMeals;
using App.Infrastructure.Builders.Common;
using App.Infrastructure.ServiceHandler.Tour;
using App.Infrastructure.Utility.Common;
using Hangfire;
using Microsoft.AspNetCore.Html;
using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Infrastructure.ServiceHandler.TravelMeals
{

    public class EmailUtils
    {
        public static async Task EmailBoss(TrDbRestaurantBooking booking, DbShop shopInfo, string tempName, string wwwPath,string subject, ITwilioUtil _twilioUtil, IContentBuilder _contentBuilder, decimal exRate, ILogManager _logger)
        {
            string htmlTemp = EmailTemplateUtil.ReadTemplate(wwwPath, tempName);
            string currencyStr = booking.PayCurrency == "UK" ? "￡" : "€";
            string Detail = "";
            foreach (var item in booking.Details)
            {
                decimal amount = 0;
                decimal paidAmount = 0; //item.AmountInfos.Sum(x => x.PaidAmount);

               
                paidAmount = item.AmountInfos.Sum(x => x.PaidAmount);
                if (booking.PayCurrency == item.Currency)
                {
                    amount = item.AmountInfos.Sum(x => x.Amount);
                }
                else if (booking.PayCurrency == "UK")
                {
                    amount = item.AmountInfos.Sum(x => x.Amount)* exRate;
                }
                else
                {
                    amount = item.AmountInfos.Sum(x => x.Amount)/ exRate;
                }
                paidAmount = Math.Round(paidAmount, 2);
                amount = Math.Round(amount, 2);
                Detail = "";
                foreach (var course in item.Courses)
                {
                    Detail += $"{course.MenuItemName} * {course.Qty} 人 {currencyStr}{paidAmount}/{amount}<br>";
                }
                //_twilioUtil.sendSMS(item.RestaurantPhone, "You got a new order. Please see details in groupmeals.com");
                Detail += $"Amount(金额)：<b>{currencyStr}{amount}</b>, Paid(已付)：<b>{currencyStr}{paidAmount}</b>, UnPaid(待支付)：<b style=\"color: red;\">{currencyStr}{amount - paidAmount}</b>";
                var detailstr = new HtmlString(Detail);
                var emailHtml = await _contentBuilder.BuildRazorContent(new { booking = booking, bookingDetail = item, AmountStr= amount, PaidAmountStr= paidAmount, UnpaidAmountStr= amount - paidAmount, Detail = detailstr, Memo = item.Courses[0].Memo }, htmlTemp);
                try
                {
                    BackgroundJob.Enqueue<ITourBatchServiceHandler>(s => s.SendEmail(shopInfo.ShopSettings, shopInfo.Email, item.RestaurantEmail, subject, emailHtml));
                }
                catch (Exception ex)
                {
                    _logger.LogError($"EmailBossError {ex.Message} -{ex.StackTrace} ");
                }
            }
        }

        public static async Task EmailSupport(TrDbRestaurantBooking booking, DbShop shopInfo, string tempName, string wwwPath, string subject, ITwilioUtil _twilioUtil, IContentBuilder _contentBuilder,decimal exRate, ILogManager _logger)
        {
            string htmlTemp = EmailTemplateUtil.ReadTemplate(wwwPath, tempName);
            string currencyStr = booking.PayCurrency == "UK" ? "￡" : "€";
            foreach (var item in booking.Details)
            {
                decimal amount = 0;// item.AmountInfos.Sum(x => x.Amount);
                decimal paidAmount = 0;// item.AmountInfos.Sum(x => x.PaidAmount);

              
                paidAmount = item.AmountInfos.Sum(x => x.PaidAmount);
                if (booking.PayCurrency == item.Currency)
                {
                    amount = item.AmountInfos.Sum(x => x.Amount);
                }
                else if (booking.PayCurrency == "UK")
                {
                    amount = item.AmountInfos.Sum(x => x.Amount) * exRate;

                }
                else
                {
                    amount = item.AmountInfos.Sum(x => x.Amount) / exRate;
                }
                paidAmount = Math.Round(paidAmount, 2);
                amount = Math.Round(amount, 2);
                string Detail = "";
                foreach (var course in item.Courses)
                {
                    Detail += $"{course.MenuItemName} * {course.Qty} 人 {currencyStr}{paidAmount}/{amount}<br>";
                }
                //_twilioUtil.sendSMS(booking.Details[0].RestaurantPhone, "You got a new order. Please see details in groupmeals.com");
                Detail += $"Amount(金额)：<b>{currencyStr}{amount}</b> Paid(已付)：<b>{currencyStr}{paidAmount}</b> UnPaid(待支付)：<b style=\"color: red;\">{currencyStr}{amount - paidAmount}</b>";
                var detailstr = new HtmlString(Detail);
                var emailHtml = await _contentBuilder.BuildRazorContent(new { booking = booking, bookingDetail = item, AmountStr = amount, PaidAmountStr = paidAmount, UnpaidAmountStr = amount - paidAmount, Detail = detailstr, Memo = item.Courses[0].Memo }, htmlTemp);
                try
                {
                    BackgroundJob.Enqueue<ITourBatchServiceHandler>(s => s.SendEmail(shopInfo.ShopSettings, shopInfo.Email, item.SupporterEmail, "New Booking", emailHtml));
                }
                catch (Exception ex)
                {
                    _logger.LogError($"EmailBossError {ex.Message} -{ex.StackTrace} ");
                }
            }
        }

        public static async Task EmailCustomerTotal(TrDbRestaurantBooking booking, DbShop shopInfo, string tempName, string wwwPath, string subject, IContentBuilder _contentBuilder, decimal exRate, ILogManager _logger)
        {
            string htmlTemp = EmailTemplateUtil.ReadTemplate(wwwPath, tempName);
            string Detail = "";
            decimal totalAmount = 0, totalPaidAmount = 0;
            string currencyStr = booking.PayCurrency == "UK" ? "￡" : "€";
            foreach (var item in booking.Details)
            {
                Detail += item.RestaurantName + " <br> ";
                Detail += item.RestaurantPhone + "  " + item.RestaurantEmail + " <br> ";
                Detail += item.SelectDateTime + " <br> ";
                decimal amount = item.AmountInfos.Sum(x => x.Amount);
                decimal paidAmount = item.AmountInfos.Sum(x => x.PaidAmount);


                totalPaidAmount += paidAmount;
                if (booking.PayCurrency == item.Currency)
                {
                    totalAmount += amount;
                }
                else if (booking.PayCurrency == "UK")
                {
                    totalAmount += amount * exRate;

                }
                else
                {
                    totalAmount += amount / exRate;
                }


                foreach (var course in item.Courses)
                {
                    Detail += $"{course.MenuItemName} * {course.Qty}  人 <br>  {currencyStr}{Math.Round(paidAmount, 2)}/{Math.Round(amount, 2)} <br> ";
                }
                Detail += " <br> ";
            }
            string AmountStr = currencyStr + Math.Round(totalAmount, 2);
            string PaidAmountStr = currencyStr + Math.Round(totalPaidAmount, 2);
            string UnpaidAmountStr = currencyStr + Math.Round((totalAmount - totalPaidAmount), 2);
            //Detail += $"Amount(金额)：<b>{currencyStr}{totalAmount}</b> Paid(已付)：<b>{currencyStr}{totalPaidAmount}</b> UnPaid(待支付)：<b style=\"color: red;\">{currencyStr}{totalAmount - totalPaidAmount}</b>";
            var detailstr = new HtmlString(Detail);
            var emailHtml = "";
            try
            {
                emailHtml = await _contentBuilder.BuildRazorContent(new { booking, bookingDetail = booking.Details[0], AmountStr, PaidAmountStr, UnpaidAmountStr, Detail = detailstr }, htmlTemp);
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
                BackgroundJob.Enqueue<ITourBatchServiceHandler>(s => s.SendEmail(shopInfo.ShopSettings, shopInfo.Email, booking.CustomerEmail, subject, emailHtml));

            }
            catch (Exception ex)
            {
                _logger.LogError($"EmailCustomer Email Customer Error {ex.Message} -{ex.StackTrace} ");
            }

        }
        public static async Task EmailCustomer(TrDbRestaurantBooking booking, DbShop shopInfo, string tempName, string wwwPath, string subject, IContentBuilder _contentBuilder, ILogManager _logger)
        {
            string htmlTemp = EmailTemplateUtil.ReadTemplate(wwwPath, tempName);
            string Detail = "";
            decimal totalAmount = 0, totalPaidAmount = 0;
            string currencyStr = booking.PayCurrency == "UK" ? "￡" : "€";
            foreach (var item in booking.Details)
            {
                Detail += item.RestaurantName + "       ";
                decimal amount = item.AmountInfos.Sum(x => x.Amount);
                totalAmount += amount;
                decimal paidAmount = item.AmountInfos.Sum(x => x.PaidAmount);
                totalPaidAmount += paidAmount;
                foreach (var course in item.Courses)
                {
                    Detail += $"{course.MenuItemName} * {course.Qty}  人  {currencyStr}{paidAmount}/{amount}<br>";
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
    }
}
