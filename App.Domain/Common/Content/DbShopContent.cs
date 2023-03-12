namespace App.Domain.Common.Content
{
    public class DbShopContent : DbEntity
    {
        public string Key { get; set; }
        public string Subject { get; set; }
        public string Content { get; set; }
    }
}