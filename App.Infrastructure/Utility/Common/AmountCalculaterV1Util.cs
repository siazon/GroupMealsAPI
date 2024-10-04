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
        PaymentAmountInfo GetOrderPaidInfo(List<DbBooking> details, string payCurrency, int shopId, DbCustomer customer, List<DbCountry> country);
        decimal CalculateOrderPaidAmount(List<DbBooking> details, string PayCurrency, DbCustomer customer, List<DbCountry> countries);
        decimal getItemAmount(DbBooking bookingDetail);
        decimal getItemPayAmount(DbBooking bookingDetail, double VAT = 0.125);
        decimal GetReward(decimal _amount, PaymentTypeEnum rewardType, double reward, DbCustomer user);
        decimal CalculatePayAmountByRate(decimal amount, string currency, string payCurrency, int shopId, List<DbCountry> country);
    }

    public class AmountCalculaterV1Util : IAmountCalculaterUtil
    {
        ICountryServiceHandler _countryServiceHandler;
        public AmountCalculaterV1Util(ICountryServiceHandler countryServiceHandler)
        {
            _countryServiceHandler = countryServiceHandler;
        }

        public PaymentAmountInfo GetOrderPaidInfo(List<DbBooking> details, string currency, int shopId, DbCustomer customer, List<DbCountry> countries)
        {
            PaymentAmountInfo paymentAmountInfo = new PaymentAmountInfo();
            Dictionary<string, decimal> dicAmount = new Dictionary<string, decimal>();
            Dictionary<string, decimal> dicReward = new Dictionary<string, decimal>();
            Dictionary<string, decimal> dicUnPaidAmount = new Dictionary<string, decimal>();
            Dictionary<string, DicModel> dicInfo = new Dictionary<string, DicModel>();
            bool hasFullpay = false;
            foreach (var item in details)
            {
                var amount = getItemAmount(item);//总金额
                var payAmount = getItemPayAmount(item);//线上支付金额
                var reward = GetReward(amount, item.BillInfo.RewardType, item.BillInfo.Reward, customer);
                var payAmountWithReward = payAmount - reward;//减去返钱

                paymentAmountInfo.TotalPayAmount += CalculatePayAmountByRate(payAmountWithReward, item.Currency, currency, shopId, countries);
                if (paymentAmountInfo.TotalPayAmount > 0 && !item.BillInfo.IsOldCustomer)
                {

                    if (item.BillInfo.PaymentType == PaymentTypeEnum.Full)
                    {
                        hasFullpay = true;
                    }
                }
                if (dicInfo.ContainsKey(item.Currency))
                {
                    dicInfo[item.Currency].Amount += Math.Round(amount, 2);
                    dicInfo[item.Currency].UnPaidAmount += Math.Round(amount - payAmount, 2);
                    if (reward > 0)
                        dicInfo[item.Currency].Reward += Math.Round(reward, 2);
                }
                else
                {
                    dicInfo.Add(item.Currency, new DicModel());
                    dicInfo[item.Currency].Amount = Math.Round(amount, 2);
                    dicInfo[item.Currency].UnPaidAmount = Math.Round(amount - payAmount, 2);
                    if (reward > 0)
                        dicInfo[item.Currency].Reward = Math.Round(reward, 2);
                }
                //if (dicAmount.ContainsKey(item.Currency))
                //    dicAmount[item.Currency] += Math.Round(amount, 2);
                //else
                //    dicAmount[item.Currency] = Math.Round(amount, 2);

                //if (dicUnPaidAmount.ContainsKey(item.Currency))
                //    dicUnPaidAmount[item.Currency] += Math.Round(amount - payAmount, 2);
                //else
                //    dicUnPaidAmount[item.Currency] = Math.Round(amount - payAmount, 2);
                //if (reward > 0)
                //{
                //    if (dicReward.ContainsKey(item.Currency))
                //        dicReward[item.Currency] += Math.Round(reward, 2);
                //    else
                //        dicReward[item.Currency] = Math.Round(reward, 2);
                //}
            }

            if (hasFullpay)
                paymentAmountInfo.PaymentMode = new List<int> { 2 };
            else
                paymentAmountInfo.PaymentMode = new List<int> { 1, 2 };

            paymentAmountInfo.AmountText = JionDictionary(dicInfo, countries, "amount");
            paymentAmountInfo.UnPaidAmountText = JionDictionary(dicInfo, countries, "unpaid");
            string rewardText = JionDictionary(dicInfo, countries, "reward");
            paymentAmountInfo.RewardText = rewardText;

            return paymentAmountInfo;

        }
        public decimal CalculateOrderPaidAmount(List<DbBooking> details, string PayCurrency, DbCustomer customer, List<DbCountry> countries)
        {
            decimal amount = 0;
            foreach (DbBooking item in details)
            {
                var payAmount = getItemPayAmount(item);//线上支付金额
                var _amount = getItemAmount(item);//总金额
                var reward = GetReward(_amount, item.BillInfo.RewardType, item.BillInfo.Reward, customer);
                var payAmountWithReward = payAmount - reward;//减去返钱
                var amountByRate = CalculatePayAmountByRate(payAmountWithReward, item.Currency, PayCurrency, customer.ShopId ?? 11, countries);
                amount += amountByRate;
            }
            return amount;
        }

        private string JionDictionary(Dictionary<string, DicModel> dicAmount, List<DbCountry> dbCountry, string amountType)
        {
            List<string> temp = new List<string>();
            foreach (var item in dicAmount)
            {
                string symbol = "";
                var country = dbCountry.FirstOrDefault(c => c.Currency == item.Key);
                if (country != null)
                {
                    symbol = country.CurrencySymbol;
                }
                var _amount = "";
                if (amountType == "amount")
                    _amount = item.Value.Amount.ToString("0.00");
                if (amountType == "reward" && item.Value.Reward > 0)
                    _amount = item.Value.Reward.ToString("0.00");
                if (amountType == "unpaid")
                    _amount = item.Value.UnPaidAmount.ToString("0.00");
                if (!string.IsNullOrWhiteSpace(_amount))
                    temp.Add($"{symbol} {_amount}");
            }
            string amountText = string.Join(" + ", temp);
            return amountText;
        }

        #region 按汇率计算


        public decimal CalculatePayAmountByRate(decimal oAmount, string currency, string payCurrency, int shopId, List<DbCountry> country)
        {
            decimal amount = 0;
            amount = CalculateByRate(oAmount, currency, payCurrency, country);
            return amount;
        }

        private decimal CalculateByRate(decimal oAmount, string Currency, string payCurrency, List<DbCountry> country)
        {
            decimal amount = 0;
            var exRate = country.FirstOrDefault(a => a.Currency == Currency)?.ExchangeRate ?? 1;
            var UKRate = country.FirstOrDefault(a => a.Currency == "UK" || a.Currency == "GBP")?.ExchangeRate ?? 1;
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
        public decimal GetReward(decimal _amount, PaymentTypeEnum rewardType, double reward, DbCustomer user)
        {
            decimal amount = 0;
            if (user.IsOldCustomer) return amount;
            var restaurantReward = FindValueByType(_amount, rewardType, reward);
            if (user.Reward == 0)
                return restaurantReward;
            var userReward = FindValueByType(_amount, user.RewardType, user.Reward);

            amount = restaurantReward > userReward ? restaurantReward : userReward;
            return amount;
        }

        private decimal FindValueByType(decimal amount, PaymentTypeEnum rewardType, double rate)
        {
            decimal reward = 0;
            if (rewardType == PaymentTypeEnum.Full)
            {
                return 0;
            }
            else if (rewardType == PaymentTypeEnum.Percentage)
            {
                reward = amount * (decimal)rate;
            }
            else if (rewardType == PaymentTypeEnum.Fixed)
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
            if (bookingDetail.BillInfo.IsOldCustomer)
                return 0;
            decimal amount = getItemAmount(bookingDetail);//付全额
            switch (bookingDetail.BillInfo.PaymentType)
            {
                case PaymentTypeEnum.Full:
                    return amount;
                    break;
                case PaymentTypeEnum.Percentage:
                    amount = amount * (decimal)bookingDetail.BillInfo.PayRate;
                    break;
                case PaymentTypeEnum.Fixed:
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
    public class DicModel
    {
        public decimal Amount { get; set; }
        public decimal Reward { get; set; }
        public decimal UnPaidAmount { get; set; }
    }
}
