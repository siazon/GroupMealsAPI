using App.Domain.Common.Shop;
using App.Domain.Enum;
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

    public interface IEmailUtils
    {
        Task EmailBoss(TrDbRestaurantBooking booking, DbShop shopInfo, string tempName, string wwwPath, string subject, ITwilioUtil _twilioUtil, IContentBuilder _contentBuilder, ILogManager _logger);
        //Task EmailSupport(TrDbRestaurantBooking booking, DbShop shopInfo, string tempName, string wwwPath, string subject, ITwilioUtil _twilioUtil, IContentBuilder _contentBuilder, decimal exRate, ILogManager _logger);
        Task EmailCustomerTotal(TrDbRestaurantBooking booking, DbShop shopInfo, string tempName, string wwwPath, string subject, IContentBuilder _contentBuilder, ILogManager _logger);

        Task EmailCustomer(TrDbRestaurantBooking booking, DbShop shopInfo, string tempName, string wwwPath, string subject, IContentBuilder _contentBuilder, ILogManager _logger);


    }
    public class EmailUtils
    {
        public static async Task EmailBoss(TrDbRestaurantBooking booking, DbShop shopInfo, string tempName, string wwwPath, string subject, ITwilioUtil _twilioUtil, IContentBuilder _contentBuilder, ILogManager _logger)
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
                List<string> names=new List<string>();
                int qty=item.Courses.Sum(c=>c.Qty+c.ChildrenQty);
                foreach (var course in item.Courses)
                {
                    names.Add(course.MenuItemName);
                }
                Detail += $"{string.Join('/',names)} * {qty} 人 <br>";
                string itemCurrencyStr = item.Currency == "UK" ? "￡" : "€";
                //_twilioUtil.sendSMS(item.RestaurantPhone, "You got a new order. Please see details in groupmeals.com");
                Detail += $"Amount(金额)：<b>{itemCurrencyStr}{item.AmountInfos.Sum(x => x.Amount)}</b>, <br> Paid(已付)：<b>{itemCurrencyStr}{paidAmount}</b>,<br>";
                if(amount - paidAmount>0)
                Detail += $" UnPaid(待支付)：<b style=\"color: red;\">{itemCurrencyStr}{amount - paidAmount}</b>";
                var detailstr = new HtmlString(Detail);
                var emailHtml = await _contentBuilder.BuildRazorContent(new { booking = booking, bookingDetail = item, AmountStr = amount, PaidAmountStr = paidAmount, UnpaidAmountStr = amount - paidAmount, Detail = detailstr, Memo = item.Courses[0].Memo }, htmlTemp);
                try
                {
                    BackgroundJob.Enqueue<ITourBatchServiceHandler>(s => s.SendEmail(shopInfo.ShopSettings, shopInfo.Email, item.RestaurantEmail, subject, emailHtml));
                    //BackgroundJob.Enqueue<ITourBatchServiceHandler>(s => s.SendEmail(shopInfo.ShopSettings, shopInfo.Email, item.SupporterEmail, subject, emailHtml));
                }
                catch (Exception ex)
                {
                    _logger.LogError($"EmailBossError {ex.Message} -{ex.StackTrace} ");
                }
            }
        }

        //public static async Task EmailSupport(TrDbRestaurantBooking booking, DbShop shopInfo, string tempName, string wwwPath, string subject, ITwilioUtil _twilioUtil, IContentBuilder _contentBuilder, decimal exRate, ILogManager _logger)
        //{
        //    string htmlTemp = EmailTemplateUtil.ReadTemplate(wwwPath, tempName);
        //    string currencyStr = booking.PayCurrency == "UK" ? "￡" : "€";
        //    foreach (var item in booking.Details)
        //    {
        //        decimal amount = 0;// item.AmountInfos.Sum(x => x.Amount);
        //        decimal paidAmount = 0;// item.AmountInfos.Sum(x => x.PaidAmount);


        //        paidAmount = item.AmountInfos.Sum(x => x.PaidAmount);
        //        if (booking.PayCurrency == item.Currency)
        //        {
        //            amount = item.AmountInfos.Sum(x => x.Amount);
        //        }
        //        else if (booking.PayCurrency == "UK")
        //        {
        //            amount = item.AmountInfos.Sum(x => x.Amount) * exRate;

        //        }
        //        else
        //        {
        //            amount = item.AmountInfos.Sum(x => x.Amount) / exRate;
        //        }
        //        paidAmount = Math.Round(paidAmount, 2);
        //        amount = Math.Round(amount, 2);
        //        string Detail = "";
        //        foreach (var course in item.Courses)
        //        {
        //            Detail += $"{course.MenuItemName} * {course.Qty} 人 {currencyStr}{paidAmount}/{amount}<br>";
        //        }

        //        string itemCurrencyStr = item.Currency == "UK" ? "￡" : "€";
        //        //_twilioUtil.sendSMS(booking.Details[0].RestaurantPhone, "You got a new order. Please see details in groupmeals.com");
        //        Detail += $"Amount(金额)：<b>{itemCurrencyStr}{item.AmountInfos.Sum(x => x.Amount)}</b> Paid(已付)：<b>{currencyStr}{paidAmount}</b> UnPaid(待支付)：<b style=\"color: red;\">{currencyStr}{amount - paidAmount}</b>";
        //        var detailstr = new HtmlString(Detail);
        //        var emailHtml = await _contentBuilder.BuildRazorContent(new { booking = booking, bookingDetail = item, AmountStr = amount, PaidAmountStr = paidAmount, UnpaidAmountStr = amount - paidAmount, Detail = detailstr, Memo = item.Memo }, htmlTemp);
        //        try
        //        {
        //            BackgroundJob.Enqueue<ITourBatchServiceHandler>(s => s.SendEmail(shopInfo.ShopSettings, shopInfo.Email, item.SupporterEmail, subject, emailHtml));
        //        }
        //        catch (Exception ex)
        //        {
        //            _logger.LogError($"EmailBossError {ex.Message} -{ex.StackTrace} ");
        //        }
        //    }
        //}

        public static async Task EmailCustomerTotal(TrDbRestaurantBooking booking, DbShop shopInfo, string tempName, string wwwPath, string subject, IContentBuilder _contentBuilder,  ILogManager _logger)
        {
            string htmlTemp = EmailTemplateUtil.ReadTemplate(wwwPath, tempName);
            string Detail = "";
            decimal totalAmount = 0, totalPaidAmount = 0;
            decimal UKAmount = 0, EUAmount = 0,UKUnPaidAmount=0,EUUnPaidAmount=0;
            string currencyStr = booking.PayCurrency == "UK" ? "￡" : "€";
            foreach (var item in booking.Details)
            {
                if (item.Status == OrderStatusEnum.Canceled) continue;
                Detail += item.RestaurantName +" <br> ";
                Detail += item.RestaurantAddress + " <br> ";
                Detail += item.RestaurantPhone + "  " + item.RestaurantEmail + " <br> ";
                Detail += item.SelectDateTime + " <br> ";
                decimal amount = item.AmountInfos.Sum(x => x.Amount);
                decimal paidAmount = item.AmountInfos.Sum(x => x.PaidAmount);


                totalPaidAmount += paidAmount;
                 if (item.Currency == "UK")
                {
                    UKAmount += amount ;
                    UKUnPaidAmount += amount-paidAmount;
                }
                else
                {
                    EUAmount += amount;
                    EUUnPaidAmount += amount-paidAmount;
                }

                string itemCurrencyStr = item.Currency == "UK" ? "￡" : "€";
                List<string> names =  new List<string>();
                int qty = item.Courses.Sum(c=>c.Qty+c.ChildrenQty);
                foreach (var course in item.Courses)
                {
                    names .Add( course.MenuItemName);
                }
                Detail += $"{string.Join('/',names)} * {qty}  人 <br> Amount(金额)：{itemCurrencyStr}{Math.Round(amount, 2)}，    Paid(已付){itemCurrencyStr}{Math.Round(paidAmount, 2)}, <br>";
                if (amount - paidAmount>0)
                    Detail += $"UnPaid(待支付)：<b style ='color: red;'>{itemCurrencyStr}{amount - paidAmount}</b> <br>";

                Detail += "<br>";
            }
            string PaidAmountStr = currencyStr + Math.Round(totalPaidAmount, 2);
            string UnpaidAmountStr = "";
            string AmountStr = "";

            if (UKAmount > 0 && EUAmount > 0)
                AmountStr = $"€{Math.Round(EUAmount, 2)} + ￡{Math.Round(UKAmount, 2)}";
            else
                AmountStr = UKAmount > 0 ? $"￡{Math.Round(UKAmount, 2)}" : $"€{Math.Round(EUAmount, 2)}";

            if (UKUnPaidAmount > 0 && EUUnPaidAmount > 0)
                UnpaidAmountStr = $"€{Math.Round(EUUnPaidAmount, 2)} + ￡{Math.Round(UKUnPaidAmount, 2)}";
            else
                UnpaidAmountStr = UKUnPaidAmount > 0 ? $"￡{Math.Round(UKUnPaidAmount, 2)}" : $"€{Math.Round(EUUnPaidAmount, 2)}";

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
                BackgroundJob.Enqueue<ITourBatchServiceHandler>(s => s.SendEmail(shopInfo.ShopSettings, shopInfo.Email, booking.CustomerEmail, subject, emailHtml));

            }
            catch (Exception ex)
            {
                _logger.LogError($"EmailCustomer Email Customer Error {ex.Message} -{ex.StackTrace} ");
            }
        }
    }
}
