using System;

namespace Takeaway.Service.Contract.Entities.Marketing
{
    public class WsMarketingEmailTask: WsEntity
    {
        public int? JobId { get; set; }
        public DateTime? LastExecuted { get; set; }
        public string Subject { get; set; }
        public string Content { get; set; }
    }
}