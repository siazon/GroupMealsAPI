using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Domain.TravelMeals.VO
{
    public class FCMMessage
    {
        public string MsgType { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public string DeviceToken { get; set; }
        // Add more properties as needed based on your notification requirements
    }
}
