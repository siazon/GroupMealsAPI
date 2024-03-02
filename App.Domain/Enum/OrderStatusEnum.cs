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
        None = 0,
        Paid=1,
        ApplyRefund=2,
        Refunded=3,
        Disable=4,
        PayAtProperty=5,
        PaidDeposit=6,
        PartialCanceled=7

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
