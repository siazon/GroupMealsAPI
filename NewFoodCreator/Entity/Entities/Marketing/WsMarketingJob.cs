using System;

namespace Takeaway.Service.Contract.Entities.Marketing
{
    public class WsMarketingJob: WsEntity
    {
        public DateTime? IssueDate { get; set; }
        public bool? IsActive { get; set; }
        public int? JobType { get; set; }
        public int? PromotionType { get; set; }
        public string Schedule { get; set; }

    }
}