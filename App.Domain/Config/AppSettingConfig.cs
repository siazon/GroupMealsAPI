﻿namespace App.Domain.Config
{
    public class AppSettingConfig
    {
        public string TimeZoneId { get; set; }
        public string ShopAuthKey { get; set; }
        public string ShopUrl { get; set; }
        public string SmtpKey { get; set; }
        public string StripeKey { get; set; }
        public string StripeWebhookKey { get; set; }
        public string StripeKey_live { get; set; }
        public string StripeWebhookKey_live { get; set; }
    }
}