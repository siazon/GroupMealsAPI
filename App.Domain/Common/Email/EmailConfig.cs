using App.Domain.Common.Setting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Domain.Common.Email
{
    public class EmailConfigs
    {
        private static EmailConfigs _instance;

        public static EmailConfigs Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new EmailConfigs();
                return _instance;
            }
        }

        public Dictionary<EmailTypeEnum, EmailSenderParams> Emails = new Dictionary<EmailTypeEnum, EmailSenderParams>();
        public EmailConfigs()
        {//

            Emails[EmailTypeEnum.VerifyCode] = new EmailSenderParams() { TemplateName = "verify_code", Subject = "来自Groupmeals.com的验证码",SenderEmail= "noreply@groupmeals.com", CCEmail = new List<string>() { "" } };
            Emails[EmailTypeEnum.NewMealCustomer] = new EmailSenderParams() { TemplateName = "new_meals_customer", Subject = "Thank you for your Booking", SenderEmail = "noreply@groupmeals.com", CCEmail = new List<string>() { "" } };
            Emails[EmailTypeEnum.NewMealRestaurant] = new EmailSenderParams() { TemplateName = "new_meals_restaurant", Subject = "New Booking", SenderEmail = "noreply@groupmeals.com", CCEmail = new List<string>() { "sales.ie@groupmeals.com" } };
            Emails[EmailTypeEnum.MealAccepted] = new EmailSenderParams() { TemplateName = "meal_accepted", Subject = "Your Booking has been Accepted", SenderEmail = "noreply@groupmeals.com", CCEmail = new List<string>() { "" } };
            Emails[EmailTypeEnum.MealAcceptedRestaurant] = new EmailSenderParams() { TemplateName = "meal_accepted_restaurant", Subject = "You Accepted an Booking", SenderEmail = "noreply@groupmeals.com", CCEmail = new List<string>() { "" } };
            Emails[EmailTypeEnum.MealDeclined] = new EmailSenderParams() { TemplateName = "meal_declined", Subject = "Your Booking has been Declined", SenderEmail = "noreply@groupmeals.com", CCEmail = new List<string>() { "sales.ie@groupmeals.com" } };
            Emails[EmailTypeEnum.MealModified] = new EmailSenderParams() { TemplateName = "meal_modify", Subject = "(订单修改)Booking Modified", SenderEmail = "noreply@groupmeals.com", CCEmail = new List<string>() { "sales.ie@groupmeals.com" } };
            Emails[EmailTypeEnum.MealCancelled] = new EmailSenderParams() { TemplateName = "meal_canceled", Subject = "(订单取消)Booking Canceled", SenderEmail = "noreply@groupmeals.com", CCEmail = new List<string>() { "sales.ie@groupmeals.com" } };
        }
    }
    public class EmailSenderParams
    {
        public string TemplateName { get; set; }
        public string Subject { get; set; }
        public List<DbSetting> ShopSettings { get; set; }
        public string SenderEmail { get; set; }
        public List<string> CCEmail { get; set; } = new List<string>();

        public string BookingRef { get; set; }
        public string BookingId { get; set; }
        public object PaidAmount { get; set; }
        public string UnPaidAmount { get; set; }
        public string Amount { get; set; }
        public object RestaurantInfo { get; set; }
        public int isShortInfo { get; set; }
        public object CustomerInfo { get; set; }
        public string MealTime { get; set; }
        public object Details { get; set; }
        public string Memo { get; set; }
        public string ReceiverEmail { get; set; }


    }

    public enum EmailTypeEnum
    {
        VerifyCode,
        NewMealCustomer,
        NewMealRestaurant,
        MealAccepted,
        MealAcceptedRestaurant,
        MealDeclined,
        MealModified,
        MealCancelled,
        MealRefunded,
        SystemMsg,

    }
}
