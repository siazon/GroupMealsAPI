namespace App.Domain.Common.Shop
{
    public class DbShopLink
    {
        public string ShopMenuLink { get; set; }
        public string ShopFacebook { get; set; }
        public string ShopLinkedIn { get; set; }
        public string Wechat { get; set; }
        public string WhatsApp { get; set; }
        public string ShopInstagram { get; set; }
        public string FacebookEmbed { get; set; }

        ///Enter by Admin
        public string IoSlink { get; set; }

        ///Enter by Admin
        public string Androidlink { get; set; }
    }
}