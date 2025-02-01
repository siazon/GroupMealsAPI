using App.Domain.Common;
using App.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Domain.TravelMeals.VO
{
    public class FCMMessage
    {
        public MsgTypeEnum MsgType { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public string ReferenceId { get; set; }
        public OrderStatusEnum OrderStatus { get; set; }
        public string DeviceToken { get; set; }
        // Add more properties as needed based on your notification requirements
    }
}
