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
        None = 0,//支付失败
        UnAccepted=1,
        Accepted=2,
        Canceled=3,
        OpenOrder = 4,
        Settled=5,

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
