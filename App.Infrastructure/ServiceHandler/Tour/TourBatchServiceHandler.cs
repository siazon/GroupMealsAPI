using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using App.Domain.Common.Setting;
using App.Infrastructure.Utility.Common;

namespace App.Infrastructure.ServiceHandler.Tour
{
    public interface ITourBatchServiceHandler
    {
        
        Task<bool> SendEmail(List<DbSetting> settings, string fromEmail,  string toEmail, string subject, string bodyHtml);
    }

    public class TourBatchServiceHandler : ITourBatchServiceHandler
    {
        private readonly IEmailUtil _emailUtil;

        public TourBatchServiceHandler(IEmailUtil emailUtil)
        {
            _emailUtil = emailUtil;
        }

        public async Task<bool> SendEmail(List<DbSetting> settings, string fromEmail, string toEmail,
            string subject, string bodyHtml)
        {
            return await _emailUtil.SendEmail(settings, fromEmail, null, toEmail, null, subject, null,
                bodyHtml);
        }
    }
}