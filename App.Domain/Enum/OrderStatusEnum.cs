using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Domain.Enum
{
    public enum OrderStatusEnum
    {
        [Description("支付失败")]
        None = 0,
        [Description("待接收")]
        UnAccepted =1,
        [Description("已接收")]
        Accepted =2,
        [Description("已取消")]
        Canceled =3,
        [Description("未结单")]
        OpenOrder = 4,
        [Description("已结单")]
        Settled =5,

    }
    public enum AcceptStatusEnum
    {
        UnAccepted=0,
        Accepted=1,
        Declined=2,
        CanceledBeforeAccepted=3,
        CanceledAfterAccepted=4,
        Settled=5,
        SettledByAdmin=6
    }
    public enum PayTypeEnum
    {
        [Description("Name With Spaces1")]
        None = 0,
        [Description("Name With Spaces1")]
        Paid = 1,
        [Description("Name With Spaces1")]
        ApplyRefund = 2
    }
}
