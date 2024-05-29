using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Rest.Verify.V2.Service;

namespace App.Infrastructure.Utility.Common
{
    public interface ITwilioUtil
    {
        bool sendSMS(string phone, string content);
    }
    public class TwilioUtil : ITwilioUtil
    {
        ILogManager _logger;
        public TwilioUtil(ILogManager logger)
        {
            _logger = logger;
        }
        public bool sendSMS(string phone, string content)
        {


            //try
            //{
            //    string accountSid = "AC2edbf7ebba55ff47906bab408e8d5e1d";
            //    string authToken = "5ded5c76459b4aa5363bac49b9b849fb";

            //    TwilioClient.Init(accountSid, authToken);


            //    var verification = VerificationResource.Create(
            //        to: "+353870647175",
            //        channel: "sms",
            //        pathServiceSid: "VA0d18e5e0b9b2f5846334087d6d692cab"
            //    );
            //}
            //catch (Exception ex)
            //{

            //}


            //#if RELEASE
            bool res = false;
            try
            {
                string accountSid = "AC2edbf7ebba55ff47906bab408e8d5e1d"; //Environment.GetEnvironmentVariable("AC0d14be935864d72c96a971861b1ef75b");
                string authToken = "5ded5c76459b4aa5363bac49b9b849fb";// Environment.GetEnvironmentVariable("334ac4ae80c6e56a092fbd67bcfa35c9");

                TwilioClient.Init(accountSid, authToken);

                var message = MessageResource.Create(
                    body: content,
                    from: new Twilio.Types.PhoneNumber("+15713215092"),
                    to: new Twilio.Types.PhoneNumber(phone)
                );
                res=true;
            }
            catch (Exception e)
            {
                _logger.LogError("sendSMS: " + e.Message + e.StackTrace);
            }
            return res;
//#endif
        }
    }
}
