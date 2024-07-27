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

            Emails[EmailTypeEnum.VerifyCode] = new EmailSenderParams() { TemplateName = "verify_code", Subject = "来自Groupmeals.com的验证码", CCEmail = new List<string>() { "" } };
            Emails[EmailTypeEnum.NewMealCustomer] = new EmailSenderParams() { TemplateName = "new_meals_customer", Subject = "Thank you for your Booking", CCEmail = new List<string>() { "" } };
            Emails[EmailTypeEnum.NewMealRestaurant] = new EmailSenderParams() { TemplateName = "new_meals_restaurant", Subject = "New Booking", CCEmail = new List<string>() { "sales.ie@groupmeals.com" } };
            Emails[EmailTypeEnum.MealAccepted] = new EmailSenderParams() { TemplateName = "meal_accepted", Subject = "Your Booking has been Accepted", CCEmail = new List<string>() { "" } };
            Emails[EmailTypeEnum.MealAcceptedRestaurant] = new EmailSenderParams() { TemplateName = "meal_accepted_restaurant", Subject = "You Accepted an Booking", CCEmail = new List<string>() { "" } };
            Emails[EmailTypeEnum.MealDeclined] = new EmailSenderParams() { TemplateName = "meal_declined", Subject = "Your Booking has been Declined", CCEmail = new List<string>() { "" } };
            Emails[EmailTypeEnum.MealModified] = new EmailSenderParams() { TemplateName = "meal_modify", Subject = "Groupmeals Booking Modified", CCEmail = new List<string>() { "sales.ie@groupmeals.com" } };
            Emails[EmailTypeEnum.MealCancelled] = new EmailSenderParams() { TemplateName = "meal_canceled", Subject = "Groupmeals Booking Canceled", CCEmail = new List<string>() { "sales.ie@groupmeals.com" } };
            Emails[EmailTypeEnum.NewMealCustomer_V2] = new EmailSenderParams() { TemplateName = "new_meals_customer_v2", Subject = "Thank you for your Booking", CCEmail = new List<string>() { "" } };
            Emails[EmailTypeEnum.MealModified_V2] = new EmailSenderParams() { TemplateName = "meal_modify_v2", Subject = "Groupmeals Booking Modified", CCEmail = new List<string>() { "sales.ie@groupmeals.com" } };
        }
    }
    public class EmailSenderParams
    {
        public string TemplateName { get; set; }
        public string Subject { get; set; }
        public List<string> CCEmail { get; set; } = new List<string>();
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
        NewMealCustomer_V2,
        MealModified_V2

    }
}
