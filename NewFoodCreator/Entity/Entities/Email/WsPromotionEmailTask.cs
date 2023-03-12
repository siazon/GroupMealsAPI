namespace Takeaway.Service.Contract.Entities.Email
{
    public class WsPromotionEmailTask: WsEntity
    {
        public int? JobId { get; set; }
        public string TemplateKeyName { get; set; }
    }
}