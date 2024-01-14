using System;
using System.Collections.Generic;
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
        PaidDeposit=6
    }
}
