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

        public decimal getDiscount(BookingDetail bookingDetail)
        {
            decimal discount = 0;
            foreach (var item in bookingDetail.Courses)
            {
                if (item.Qty == 4 || item.Qty == 5)
                {
                    discount += 10 * item.Price * 0.2m;
                }
                else if (item.Qty == 6 || item.Qty == 7)
                {
                    discount += 10 * item.Price * 0.15m;
                }
                else if (item.Qty == 8)
                {
                    discount += 10 * item.Price * 0.1m;
                }
                else if (item.Qty == 9)
                {
                    discount += 10 * item.Price * 0.05m;
                }
                else
                {
                    discount += 0;
                }
            }
            return discount;
        }

        public decimal getItemAmount(BookingDetail bookingDetail)
        {
            decimal amount = 0;
            foreach (var item in bookingDetail.Courses)
            {
                if (item.Qty < 10)
                    amount += item.Price * 10;
                else
                    amount += item.Price * item.Qty;
            }
            amount -= getDiscount(bookingDetail);
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
