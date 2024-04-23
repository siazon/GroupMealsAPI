using App.Domain.TravelMeals;
using App.Domain.TravelMeals.Restaurant;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Infrastructure.Utility.Common
{
    public interface IAmountCalculaterUtil
    {
        long CalculateOrderAmount(TrDbRestaurantBooking booking, double ExRate);
        long CalculateOrderPaidAmount(TrDbRestaurantBooking booking, double ExRate);
        decimal getItemAmount(BookingDetail bookingDetail);
        decimal getItemPayAmount(BookingDetail bookingDetail);
    }

    public class AmountCalculaterV1Util : IAmountCalculaterUtil
    {
        public long CalculateOrderAmount(TrDbRestaurantBooking booking, double ExRate)
        {
            decimal amount = 0;
            if (booking != null)
            {
                foreach (var course in booking.Details)
                {
                    if (course.Currency == booking.PayCurrency)
                    {
                        amount += getItemAmount(course);
                    }
                    else
                    {
                        if (booking.PayCurrency == "UK")
                        {
                            amount += getItemAmount(course) * (decimal)ExRate;
                        }
                        else
                            amount += getItemAmount(course) / (decimal)ExRate;
                    }
                }
            }
            decimal temp = Math.Round(amount, 2);
            return (long)(temp * 100);
        }
        public long CalculateOrderPaidAmount(TrDbRestaurantBooking booking, double ExRate)
        {
            decimal amount = 0;
            if (booking != null)
            {
                foreach (var course in booking.Details)
                {
                    if (course.Currency == booking.PayCurrency)
                    {
                        amount += getItemPayAmount(course);
                    }
                    else
                    {
                        if (booking.PayCurrency == "UK")
                        {
                            amount += getItemPayAmount(course) * (decimal)ExRate;
                        }
                        else
                            amount += getItemPayAmount(course) / (decimal)ExRate;
                    }
                }
            }
            decimal temp = Math.Round(amount, 2);
            return (long)(temp * 100);
        }

        private decimal getDiscount(int qty,decimal price)
        {
            decimal discount = 0;
             if (qty == 4 || qty == 5)
            {
                discount += 10 * price * 0.2m;
            }
            else if (qty == 6 || qty == 7)
            {
                discount += 10 * price * 0.15m;
            }
            else if (qty == 8)
            {
                discount += 10 * price * 0.1m;
            }
            else if (qty == 9)
            {
                discount += 10 * price * 0.05m;
            }
            else
            {
                discount += 0;
            }
            return discount;
        }

        public decimal getItemAmount(BookingDetail bookingDetail)
        {
            decimal amount = 0;
            foreach (var item in bookingDetail.Courses)
            {
                int totalQty = item.Qty+item.ChildrenQty;
                if (item.MenuCalculateType == MenuCalculateTypeEnum.WesternFood)//西餐按人头算
                {
                    amount += item.Price * item.Qty;
                }
                else
                {
                    //if (item.Price == item.ChildrenPrice)
                    //{//价格相等等于没有儿童价格
                    //    qty = qty + item.ChildrenQty;
                    //}
                    //else
                    //{//价格不相等，儿童单价单独计算
                    //    amount += item.ChildrenPrice * item.ChildrenQty;
                    //}

                    if (totalQty < 10)
                        amount += item.Price * 10;
                    else
                        amount += item.Price * totalQty;

                    amount -= getDiscount(totalQty, item.Price);
                }
            }

         
            return amount;
        }

        public decimal getItemPayAmount(BookingDetail bookingDetail)
        {
            decimal amount = getItemAmount(bookingDetail);//付全额
            if (bookingDetail.BillInfo.PaymentType == PaymentTypeEnum.Deposit)//付押金
            {
                amount = amount * (decimal)bookingDetail.BillInfo.PayRate;
            }
            else if (bookingDetail.BillInfo.PaymentType == PaymentTypeEnum.PayAtStore)//到店付
            {
                amount = 0;
            }
            return amount;
        }
    }
}
