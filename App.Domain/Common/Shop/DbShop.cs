using App.Domain.Common.Content;
using App.Domain.Common.Setting;
using System.Collections.Generic;

namespace App.Domain.Common.Shop
{
    public class DbShop : DbEntity
    {
        public string ShopName { get; set; }
        public string ShopNumber { get; set; }
        public string ShopWeChat { get; set; }
        public string ShopWeChatQRCode { get; set; }
        public string ShopNumber2 { get; set; }
        public string ShopMobile { get; set; }
        public string Email { get; set; }
        public string ContactEmail { get; set; }

        public string Address { get; set; }

        public string Website { get; set; }

        public string ShopOpenHours { get; set; }
        public string NotificationEmail { get; set; }

        public string TokenKey { get; set; }

        public string AdminTokenKey { get; set; }
        public double ExchangeRate { get; set; }

        //CountryIdEnum
        public int? CountryId { get; set; }

        public List<DbShopContent> ShopContents { get; set; }

        public List<DbSetting> ShopSettings { get; set; }

        public DbShop()
        {
            ShopContents = new List<DbShopContent>();
            ShopSettings = new List<DbSetting>();
        }
    }

    public static class DbShopExt
    {
        public static DbShop ClearForOutPut(this DbShop source)
        {
            source.TokenKey = "";
            source.AdminTokenKey = "";
            source.ShopContents = null;
            source.ShopSettings = null;
            source.CountryId = null;
            return source;
        }

        public static DbShop Clone(this DbShop source)
        {
            var dest = new DbShop();

            dest.ShopName = source.ShopName;
            dest.ShopNumber = source.ShopNumber;
            dest.ShopWeChat = source.ShopWeChat;
            dest.ShopWeChatQRCode = source.ShopWeChatQRCode;
            dest.ShopNumber2 = source.ShopNumber2;
            dest.ShopMobile = source.ShopMobile;
            dest.Email = source.Email;
            dest.ContactEmail = source.ContactEmail;
            dest.Website = source.Website;
            dest.ShopOpenHours = source.ShopOpenHours;
            dest.Address = source.Address;
            dest.NotificationEmail = source.NotificationEmail;

            return dest;
        }

        public static DbShop Copy(this DbShop source, DbShop copyValue)
        {
            source.ShopName = copyValue.ShopName;
            source.ShopNumber = copyValue.ShopNumber;
            source.ShopWeChat = copyValue.ShopWeChat;
            source.ShopWeChatQRCode = copyValue.ShopWeChatQRCode;
            source.ShopNumber2 = copyValue.ShopNumber2;
            source.ShopMobile = copyValue.ShopMobile;
            source.Email = copyValue.Email;
            source.ContactEmail = copyValue.ContactEmail;
            source.Website = copyValue.Website;
            source.ShopOpenHours = copyValue.ShopOpenHours;
            source.Address = copyValue.Address;
            source.NotificationEmail = copyValue.NotificationEmail;

            return source;
        }
    }
}