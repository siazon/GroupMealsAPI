using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace App.Infrastructure.Utility.Common
{
    public static class TwilioUtil
    {
        public static void sendSMS(string phone,string content)
        {
            string accountSid = "AC0d14be935864d72c96a971861b1ef75b"; //Environment.GetEnvironmentVariable("AC0d14be935864d72c96a971861b1ef75b");
            string authToken = "334ac4ae80c6e56a092fbd67bcfa35c9";// Environment.GetEnvironmentVariable("334ac4ae80c6e56a092fbd67bcfa35c9");

            TwilioClient.Init(accountSid, authToken);

            var message = MessageResource.Create(
                body: content,
                from: new Twilio.Types.PhoneNumber("+12296964965"),
                to: new Twilio.Types.PhoneNumber(phone)
            );
        }
    }
}
