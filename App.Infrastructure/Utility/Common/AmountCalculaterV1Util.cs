using App.Domain.Common.Customer;
using App.Domain.TravelMeals;
using App.Domain.TravelMeals.Restaurant;
using App.Domain.TravelMeals.VO;
using App.Infrastructure.ServiceHandler.Common;
using Microsoft.Azure.Cosmos;
using Stripe;
using Stripe.FinancialConnections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Pipelines.Sockets.Unofficial.SocketConnection;
using static SendGrid.SendGridClient;

namespace App.Infrastructure.Utility.Common
{
    public interface IAmountCalculaterUtil
    {
        PaymentAmountInfo GetOrderPaidInfo(List<DbBooking> details, string payCurrency, int shopId, DbCustomer customer, DbCountry country);
        decimal CalculateOrderPaidAmount(List<DbBooking> details, string PayCurrency, DbCustomer customer);
        decimal getItemAmount(DbBooking bookingDetail);
        decimal getItemPayAmount(DbBooking bookingDetail, double VAT = 0.125);
        decimal GetReward(DbBooking detail, DbCustomer user);
        decimal CalculatePayAmountByRate(decimal amount, string currency, string payCurrency, int shopId, DbCountry country);
    }

    public class AmountCalculaterV1Util : IAmountCalculaterUtil
    {
        ICountryServiceHandler _countryServiceHandler;
        public AmountCalculaterV1Util(ICountryServiceHandler countryServiceHandler)
        {
            _countryServiceHandler = countryServiceHandler;
        }

        public PaymentAmountInfo GetOrderPaidInfo(List<DbBooking> details, string currency, int shopId, DbCustomer customer, DbCountry countries)
        {
            PaymentAmountInfo paymentAmountInfo = new PaymentAmountInfo();
            Dictionary<string, decimal> dicAmount = new Dictionary<string, decimal>();
            Dictionary<string, decimal> dicReward = new Dictionary<string, decimal>();
            Dictionary<string, decimal> dicUnPaidAmount = new Dictionary<string, decimal>();
            foreach (var item in details)
            {
                var amount = getItemAmount(item);//总金额
                var payAmount = getItemPayAmount(item);//线上支付金额
                var reward = GetReward(item,  customer);
                var payAmountWithReward = payAmount - reward;//减去返钱

                paymentAmountInfo.TotalPayAmount += CalculatePayAmountByRate(payAmountWithReward, item.Currency, currency, shopId, countries);
                if (dicAmount.ContainsKey(item.RestaurantCountry))
                    dicAmount[item.RestaurantCountry] += Math.Round(amount, 2);
                else
                    dicAmount[item.RestaurantCountry] = Math.Round(amount, 2);

                if (dicUnPaidAmount.ContainsKey(item.RestaurantCountry))
                    dicUnPaidAmount[item.RestaurantCountry] += Math.Round(amount - payAmount, 2);
                else
                    dicUnPaidAmount[item.RestaurantCountry] = Math.Round(amount - payAmount, 2);


                if (reward > 0)
                {
                    if (dicReward.ContainsKey(item.RestaurantCountry))
                        dicReward[item.RestaurantCountry] += Math.Round(reward, 2);
                    else
                        dicReward[item.RestaurantCountry] = Math.Round(reward, 2);
                }
            }

            paymentAmountInfo.AmountText = JionDictionary(dicAmount, countries);
            paymentAmountInfo.UnPaidAmountText = JionDictionary(dicUnPaidAmount, countries);
            string rewardText = JionDictionary(dicReward, countries);
            paymentAmountInfo.RewardText = string.IsNullOrWhiteSpace(rewardText) ? "暂无优惠" : rewardText;

            return paymentAmountInfo;

        }
        public decimal CalculateOrderPaidAmount(List<DbBooking> details, string PayCurrency,  DbCustomer customer )
        {
            decimal amount = 0;
            foreach (DbBooking item in details)
            {
                var payAmount = getItemPayAmount(item);//线上支付金额
                var reward = GetReward(item, customer);
                var payAmountWithReward = payAmount - reward;//减去返钱
                amount += payAmountWithReward;
            }

            return amount;
        }

        private string JionDictionary(Dictionary<string, decimal> dicAmount, DbCountry dbCountry)
        {
            List<string> temp = new List<string>();
            foreach (var item in dicAmount)
            {
                string symbol = "";
                var country = dbCountry.Countries.FirstOrDefault(c => c.Name == item.Key);
                if (country != null)
                {
                    symbol = country.CurrencySymbol;
                }
                var _amount = item.Value.ToString("0.00");
                temp.Add($"{symbol} {_amount}");
            }
            string amountText = string.Join(" + ", temp);
            return amountText;
        }

        #region 按汇率计算


        public decimal CalculatePayAmountByRate(decimal oAmount, string currency, string payCurrency, int shopId, DbCountry country)
        {
            decimal amount = 0;
            var exRate = country.Countries.FirstOrDefault(a => a.Currency == currency)?.ExchangeRate ?? 1;
            var UKRate = country.Countries.FirstOrDefault(a => a.Currency == "UK")?.ExchangeRate ?? 1;
            amount = CalculateByRate(oAmount, currency, payCurrency, country);
            return amount;
        }

        private decimal CalculateByRate(decimal oAmount, string Currency, string payCurrency, DbCountry country)
        {
            decimal amount = 0;
            var exRate = country.Countries.FirstOrDefault(a => a.Currency == Currency)?.ExchangeRate ?? 1;
            var UKRate = country.Countries.FirstOrDefault(a => a.Currency == "UK")?.ExchangeRate ?? 1;
            if (Currency == payCurrency)
            {
                amount = oAmount;
            }
            else
            {
                if (payCurrency == "UK" || payCurrency == "GBP")
                {
                    amount = oAmount * (decimal)UKRate / (decimal)exRate;
                }
                else
                    amount = oAmount / (decimal)exRate;
            }
            return amount;
        }
        public decimal GetReward(DbBooking detail,  DbCustomer user )
        {
            decimal amount = 0;
            var _amount = getItemAmount(detail);
            var restaurantReward = FindValueByType(_amount, detail.BillInfo.RewardType, detail.BillInfo.Reward);
            if (user.Reward == 0)
                return restaurantReward;
            var userReward = FindValueByType(_amount, user.RewardType, user.Reward);

            amount = restaurantReward > userReward ? restaurantReward : userReward;
            return amount;
        }

        private decimal FindValueByType(decimal amount, PaymentTypeEnum paymentType, double rate)
        {
            decimal reward = 0;
            if (paymentType == PaymentTypeEnum.Percentage)
            {
                reward = amount * (decimal)rate;
            }
            else if (paymentType == PaymentTypeEnum.Fixed)
            {
                reward = (decimal)rate;
            }
            return reward;
        }

        #endregion

        #region 单项计算


        /// <summary>
        /// 计算支付金额
        /// </summary>
        /// <param name="bookingDetail"></param>
        /// <returns></returns>
        public decimal getItemPayAmount(DbBooking bookingDetail, double VAT = 0.125)
        {
            decimal amount = getItemAmount(bookingDetail);//付全额
            switch (bookingDetail.BillInfo.PaymentType)
            {
                case PaymentTypeEnum.Full:
                    break;
                case PaymentTypeEnum.Percentage:
                    if (bookingDetail.BillInfo.IsOldCustomer)
                        amount = 0;
                    else
                        amount = amount * (decimal)bookingDetail.BillInfo.PayRate;
                    break;
                case PaymentTypeEnum.Fixed:
                    if (bookingDetail.BillInfo.IsOldCustomer)
                        amount = 0;
                    else
                        amount = (decimal)bookingDetail.BillInfo.PayRate;
                    break;
                default:
                    break;
            }
            amount *= (decimal)(1 + VAT);
            return amount;
        }

        public decimal getItemAmount(DbBooking bookingDetail)
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

                    amount -= getPackageOffer(totalQty, item.Price);
                }
            }
            return amount;
        }
        private decimal getPackageOffer(int qty, decimal price)
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

        #endregion
    }
}
