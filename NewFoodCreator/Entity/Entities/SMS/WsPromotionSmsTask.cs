namespace Takeaway.Service.Contract.Entities.SMS
{
    public class WsPromotionSmsTask: WsEntity
    {
        public int? JobId { get; set; }
        public string TemplateKeyName { get; set; }
        
    }

    
}