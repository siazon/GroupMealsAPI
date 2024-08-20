using App.Domain.TravelMeals;
using App.Domain.TravelMeals.Restaurant;
using App.Infrastructure.ServiceHandler.Common;
using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SendGrid.SendGridClient;

namespace App.Infrastructure.Utility.Common
{
    public interface IAmountCalculaterUtil
    {
        Task<long> CalculateOrderAmount(List<BookingDetail> details, string payCurrency, int shopId);
        Task<long> CalculateOrderPaidAmount(List<BookingDetail> details, string payCurrency, int shopId);
        Task<decimal> CalculateAmountByRate(BookingDetail detail, string payCurrency, int shopId, DbCountry country = null);
        Task<decimal> CalculatePayAmountByRate(BookingDetail detail, string payCurrency, int shopId, DbCountry country = null);
        decimal getItemAmount(BookingDetail bookingDetail);
        decimal getItemPayAmount(BookingDetail bookingDetail);
    }

    public class AmountCalculaterV1Util : IAmountCalculaterUtil
    {
        ICountryServiceHandler _countryServiceHandler;
        public AmountCalculaterV1Util(ICountryServiceHandler countryServiceHandler)
        {
            _countryServiceHandler = countryServiceHandler;
        }
        public async Task<decimal> CalculateByRate(Func<BookingDetail, decimal> GetAmount, BookingDetail detail, string payCurrency, int shopId, DbCountry country = null)
        {
            decimal amount = 0;
            if (country == null)
                country = await _countryServiceHandler.GetCountry(shopId);
            var exRate = country.Countries.FirstOrDefault(a => a.Currency == detail.Currency)?.ExchangeRate ?? 1;
            var UKRate = country.Countries.FirstOrDefault(a => a.Currency == "UK")?.ExchangeRate ?? 1;
            decimal oAmount = GetAmount(detail);
            if (detail.Currency == payCurrency)
            {
                amount = oAmount;
            }
            else
            {
                if (payCurrency == "UK")
                {
                    amount = oAmount * (decimal)exRate / (decimal)UKRate;
                }
                else
                    amount = oAmount / (decimal)exRate;
            }
            return amount;
        }

        public async Task<decimal> CalculateAmountByRate(BookingDetail detail, string payCurrency, int shopId, DbCountry country = null)
        {

            decimal amount = await CalculateByRate(getItemAmount, detail, payCurrency, shopId, country);

            return amount;
        }


        public async Task<decimal> CalculatePayAmountByRate(BookingDetail detail, string payCurrency, int shopId, DbCountry country = null)
        {
            decimal amount = await CalculateByRate(getItemPayAmount, detail, payCurrency, shopId, country);

            return amount;
        }
        public async Task<long> CalculateOrderAmount(List<BookingDetail> details, string payCurrency, int shopId)
        {
            decimal amount = 0;
            var country = await _countryServiceHandler.GetCountry(shopId);
            foreach (var course in details)
            {
                amount += await CalculateAmountByRate(course, payCurrency, shopId, country);
            }
            decimal temp = Math.Round(amount, 2);
            return (long)(temp * 100);
        }
        public async Task<long> CalculateOrderPaidAmount(List<BookingDetail> details, string payCurrency, int shopId)
        {
            decimal amount = 0;
            var country = await _countryServiceHandler.GetCountry(shopId);
            foreach (var course in details)
            {
                amount += await CalculatePayAmountByRate(course, payCurrency, shopId, country);
            }
            decimal temp = Math.Round(amount, 2);
            return (long)(temp * 100);
        }

        private decimal getDiscount(int qty, decimal price)
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
                int totalQty = item.Qty + item.ChildrenQty;
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
            switch (bookingDetail.BillInfo.PaymentMethod)
            {
                case PaymentMethodEnum.Full:
                    break;
                case PaymentMethodEnum.Percentage:
                    if (bookingDetail.BillInfo.IsOldCustomer)
                        amount = 0;
                    else
                        amount = amount * (decimal)bookingDetail.BillInfo.PayRate;
                    break;
                case PaymentMethodEnum.Fixed:
                    if (bookingDetail.BillInfo.IsOldCustomer)
                        amount = 0;
                    else
                        amount = (decimal)bookingDetail.BillInfo.PayRate;
                    break;
                default:
                    break;
            }
            return amount;
        }
    }
}
