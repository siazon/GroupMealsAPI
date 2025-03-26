using App.Domain.TravelMeals.VO;
using FirebaseAdmin.Messaging;
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
    public interface IFCMUtil
    {
        Task<string> SendMsg(FCMMessage FCMParams);
    }
  
    public class FCMUtil: IFCMUtil
    {
        ILogManager _logger;
        public FCMUtil(ILogManager logger)
        {
            _logger = logger;
        }
        public  async Task<string> SendMsg(FCMMessage FCMParams)
        {
            var registrationToken = FCMParams.DeviceToken;

            // See documentation on defining a message payload.
            var message = new Message()
            {
                Notification = new Notification
                {
                    Title = FCMParams.Title,
                    Body = FCMParams.Body
                },
                Token = registrationToken,
                Apns = new ApnsConfig
                {
                    Aps = new Aps
                    {
                        Badge = 1 // 设置应用图标上的角标数量
                    }
                }
            };
            try
            {
                // Send a message to the device corresponding to the provided
                // registration token.
                string response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
                // Response is a message ID string.
                _logger.LogDebug("Successfully sent FCM message: " + response);
                return "";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
    }
}
