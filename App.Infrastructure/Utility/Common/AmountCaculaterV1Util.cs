using App.Domain.TravelMeals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Infrastructure.Utility.Common
{
    public interface IAmountCaculaterUtil
    {
        long CalculateOrderAmount(TrDbRestaurantBooking booking, double ExRate);
        long CalculateOrderPaidAmount(TrDbRestaurantBooking booking, double ExRate);
        decimal getItemAmount(BookingDetail bookingDetail);
        decimal getItemPayAmount(BookingDetail bookingDetail);
    }

    public class AmountCaculaterV1Util : IAmountCaculaterUtil
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
                int qty = item.Qty;
                if (item.MenuCalculateType == 1)//西餐按人头算
                {
                    amount += item.Price * qty;
                }
                else
                {
                    if (item.Price == item.ChildrenPrice)
                    {//价格相等等于没有儿童价格
                        qty = qty + item.ChildrenQty;
                    }
                    else
                    {//价格不相等，儿童单价单独计算
                        amount += item.ChildrenPrice * item.ChildrenQty;
                    }

                    if (qty < 10)
                        amount += item.Price * 10;
                    else
                        amount += item.Price * qty;


                    amount -= getDiscount(qty, item.Price);
                }
            }

         
            return amount;
        }

        public decimal getItemPayAmount(BookingDetail bookingDetail)
        {
            decimal amount = getItemAmount(bookingDetail);
            if (bookingDetail.BillInfo.PaymentType == 1)//付押金
            {
                amount = amount * (decimal)bookingDetail.BillInfo.PayRate;
            }
            else if (bookingDetail.BillInfo.PaymentType == 2)//到店付
            {
                amount = 0;
            }
            return amount;
        }
    }
}
