using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Domain.Common
{
    public class PushMsgModel : DbEntity
    {
        public string Title { get; set; }
        public string Message { get; set; }
        public string Sender { get; set; }
        public string Receiver { get; set; }
        public MSGEnum MsgStatus { get; set; }
        public DateTime SendTime { get; set; }
    }
    public enum MSGEnum {
    defult=0,Readed=1
    }
}
