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
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Pipelines.Sockets.Unofficial.SocketConnection;
using static SendGrid.SendGridClient;

namespace App.Infrastructure.Utility.Common
{
    public interface IAmountCalculaterUtil
    {
        PaymentAmountInfo GetOrderPaidInfo(List<DbBooking> details, string payCurrency, int shopId, DbCustomer customer, List<DbCountry> country, List<DbStripeEntity> dbStripes);
        decimal CalculateOrderPaidAmount(List<DbBooking> details, string PayCurrency, DbCustomer customer, List<DbCountry> countries, List<DbStripeEntity> dbStripes);
        decimal getItemAmount(BookingCalculateVO bookingDetail);
        ItemPayInfo getItemPayAmount(BookingCalculateVO bookingDetail, DbCustomer customer, double VAT);
        decimal CalculatePayAmountByRate(decimal amount, string currency, string payCurrency, int shopId, List<DbCountry> countries, List<DbStripeEntity> dbStripes);
    }

    public class AmountCalculaterV1Util : IAmountCalculaterUtil
    {
        ICountryServiceHandler _countryServiceHandler;
        public AmountCalculaterV1Util(ICountryServiceHandler countryServiceHandler)
        {
            _countryServiceHandler = countryServiceHandler;
        }
        private Dictionary<string, Dictionary<string, decimal>> getExchangeRate(List<DbCountry> countries, List<DbStripeEntity> dbStripes)
        {
            Dictionary<string, Dictionary<string, decimal>> Rates = new Dictionary<string, Dictionary<string, decimal>>();
            Dictionary<string, decimal> rates = new Dictionary<string, decimal>();
            foreach (DbCountry country in countries)
            {
                rates = new Dictionary<string, decimal>();
                var _rates = dbStripes.FirstOrDefault(a => a.Currency == country.Currency);
                string currencies = "";
                dbStripes.ForEach(a => currencies += a.Currency + ",");
                foreach (var item in _rates.ExchangeRate.conversion_rates)
                {
                    if (!currencies.Contains(item.Key))
                        continue;
                    if (rates.ContainsKey(item.Key))
                    {
                        if (item.Key == country.Currency)
                            rates[item.Key] = 1;
                        else
                            rates[item.Key] = decimal.Parse(item.Value) + (decimal)country.ExchangeRateExtra;
                    }
                    else
                    {
                        if (item.Key == country.Currency)
                            rates[item.Key] = 1;
                        else
                            rates.Add(item.Key, decimal.Parse(item.Value) + (decimal)country.ExchangeRateExtra);
                    }
                }
                if (Rates.ContainsKey(country.Currency))
                    Rates[country.Currency] = rates;
                else
                    Rates.Add(country.Currency, rates);
            }
            return Rates;
        }
        public PaymentAmountInfo GetOrderPaidInfo(List<DbBooking> details, string currency, int shopId, DbCustomer customer, List<DbCountry> countries, List<DbStripeEntity> dbStripes)
        {
            PaymentAmountInfo paymentAmountInfo = new PaymentAmountInfo();
            Dictionary<string, decimal> dicAmount = new Dictionary<string, decimal>();
            Dictionary<string, decimal> dicReward = new Dictionary<string, decimal>();
            Dictionary<string, decimal> dicUnPaidAmount = new Dictionary<string, decimal>();
            Dictionary<string, DicModel> dicInfo = new Dictionary<string, DicModel>();
            bool hasFullpay = false;

            var Rates = getExchangeRate(countries, dbStripes);

            foreach (var item in details)
            {
                var amount = getItemAmount(item.ConvertToAmount());//总金额
                var country = countries.FirstOrDefault(a => a.Code == item.RestaurantCountry);
                var payAmount = getItemPayAmount(item.ConvertToAmount(), customer, country.VAT);//线上支付金额
                var reward = Math.Round(payAmount.Reward, 2, MidpointRounding.ToNegativeInfinity);
                if (currency == null) currency = item.Currency;
                paymentAmountInfo.TotalPayAmount += CalculatePayAmountByRate(payAmount.PayAmount, item.Currency, currency, shopId, countries, dbStripes);
                paymentAmountInfo.Amount += CalculateByRateByInOut(amount, Rates[item.Currency][currency]);
                paymentAmountInfo.UnPaidAmount += CalculateByRateByInOut(amount - payAmount.PayAmount - reward, Rates[item.Currency][currency]);
                paymentAmountInfo.Reward += CalculateByRateByInOut(reward, Rates[item.Currency][currency]);
                if (paymentAmountInfo.TotalPayAmount > 0 && !item.BillInfo.IsOldCustomer)
                {

                    if (item.BillInfo.PaymentType == PaymentTypeEnum.Full)
                    {
                        hasFullpay = true;
                    }
                }
                if (!dicInfo.ContainsKey(item.Currency))
                {
                    dicInfo.Add(item.Currency, new DicModel());
                }
                dicInfo[item.Currency].Amount = Math.Round(amount, 2);
                if (item.RestaurantIncluedVAT)
                    dicInfo[item.Currency].UnPaidAmount = Math.Round(amount - payAmount.PayAmount - reward, 2);
                else
                    dicInfo[item.Currency].UnPaidAmount = Math.Round(amount - payAmount.PayAmount - reward, 2);
                if (reward > 0)
                    dicInfo[item.Currency].Reward = Math.Round(reward, 2);

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
            if (paymentAmountInfo.TotalPayAmount < 0.5m)
                paymentAmountInfo.TotalPayAmount = 0;//不收0.5以下的钱

            paymentAmountInfo.Amount = Math.Round(paymentAmountInfo.Amount, 2, MidpointRounding.ToPositiveInfinity);
            paymentAmountInfo.TotalPayAmount = Math.Round(paymentAmountInfo.TotalPayAmount, 2, MidpointRounding.ToPositiveInfinity);
            paymentAmountInfo.UnPaidAmount = Math.Round(paymentAmountInfo.UnPaidAmount, 2, MidpointRounding.ToNegativeInfinity);
            paymentAmountInfo.Reward = Math.Round(paymentAmountInfo.Reward, 2, MidpointRounding.ToNegativeInfinity);


            if (hasFullpay || customer.IsOldCustomer)
                paymentAmountInfo.IntentType = new List<IntentTypeEnum> { IntentTypeEnum.PaymentIntent };
            else
                paymentAmountInfo.IntentType = new List<IntentTypeEnum> { IntentTypeEnum.SetupIntent };

            paymentAmountInfo.Currencys = new List<string>() { "EUR", "GBP", "USD" };

            paymentAmountInfo.AmountText = JionDictionary(dicInfo, countries, "amount");
            paymentAmountInfo.UnPaidAmountText = JionDictionary(dicInfo, countries, "unpaid");
            string rewardText = JionDictionary(dicInfo, countries, "reward");
            paymentAmountInfo.RewardText = rewardText;
            paymentAmountInfo.AmountList = dicAmount;
            paymentAmountInfo.UnPaidAmountList = dicUnPaidAmount;
            return paymentAmountInfo;

        }
        public decimal CalculateOrderPaidAmount(List<DbBooking> details, string PayCurrency, DbCustomer customer, List<DbCountry> countries, List<DbStripeEntity> dbStripes)
        {
            decimal amount = 0;
            foreach (var item in details)
            {
                var country = countries.FirstOrDefault(a => a.Code == item.RestaurantCountry);
                var payAmount = getItemPayAmount(item.ConvertToAmount(), customer, country.VAT);//线上支付金额
                var _amount = getItemAmount(item.ConvertToAmount());//总金额
                var reward = payAmount.Reward;
                var amountByRate = CalculatePayAmountByRate(payAmount.PayAmount, item.Currency, PayCurrency, customer.ShopId ?? 11, countries, dbStripes);
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


        public decimal CalculatePayAmountByRate(decimal oAmount, string currency, string payCurrency, int shopId, List<DbCountry> countries, List<DbStripeEntity> dbStripes)
        {

            decimal amount = 0;
            if (payCurrency == null)
                return 0;
            //Dictionary<string, decimal> ExchangeRates = new Dictionary<string, decimal>();
            //foreach (var item in country)
            //{
            //    if (ExchangeRates.ContainsKey(item.Currency))
            //        ExchangeRates[item.Currency] = (decimal)item.ExchangeRate;
            //    else
            //        ExchangeRates.Add(item.Currency, (decimal)item.ExchangeRate);
            //}
            var Rates = getExchangeRate(countries, dbStripes);
            amount = CalculateByRateByInOut(oAmount, Rates[currency][payCurrency]);
            return amount;
        }
        private decimal CalculateByRateByInOut(decimal oAmount, decimal ExchangeRate)
        {
            return oAmount * ExchangeRate;
        }

        private decimal CalculateByRate(decimal oAmount, string Currency, string payCurrency, Dictionary<string, decimal> ExchangeRates)
        {
            if (payCurrency == null)
                return 0;
            if (!ExchangeRates.ContainsKey(Currency))
            {
                throw new ArgumentException($"Source currency '{Currency}' is not supported.");
            }
            if (!ExchangeRates.ContainsKey(payCurrency))
            {
                throw new ArgumentException($"Target currency '{payCurrency}' is not supported.");
            }

            decimal amountInEuro = oAmount / ExchangeRates[Currency];

            decimal convertedAmount = amountInEuro * ExchangeRates[payCurrency];

            return convertedAmount;
        }
        private decimal GetReward(decimal _amount, PaymentTypeEnum rewardType, double reward, DbCustomer user, bool RestaurantIncluedVAT, decimal vat)
        {
            if (user.IsOldCustomer)
                return 0;
            decimal amount = 0;
            var restaurantReward = FindValueByType(_amount, rewardType, reward);
            if (user.Reward == 0)
            { amount = restaurantReward; }
            else
            {
                var userReward = FindValueByType(_amount, user.RewardType, user.Reward);
                amount = restaurantReward > userReward ? restaurantReward : userReward;
            }
            if (amount > 0 && vat > 0 && !RestaurantIncluedVAT)
                amount -= vat;
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
                reward = (decimal)rate * 100;
            }
            return reward;
        }

        #endregion

        #region 单项计算


        /// <summary>
        /// 计算应付金额
        /// </summary>
        /// <param name="bookingDetail"></param>
        /// <returns></returns>
        public ItemPayInfo getItemPayAmount(BookingCalculateVO bookingDetail, DbCustomer customer, double VAT)
        {
            decimal _amount = 0;
            decimal amount = getItemAmount(bookingDetail);//总金额额
            if (customer.IsOldCustomer)
            {
                if (bookingDetail.BillInfo.PaymentType == PaymentTypeEnum.Full)
                    return new ItemPayInfo() { PayAmount = amount };//  amount;
                else
                    return new ItemPayInfo();
            }
            decimal Commission = GetDue(bookingDetail, amount);//应付
            var vat = GetVATAmount(Commission, VAT);
            var reward = GetReward(amount, bookingDetail.BillInfo.RewardType, bookingDetail.BillInfo.Reward, customer, bookingDetail.RestaurantIncluedVAT, vat);//
            reward = Math.Round(reward, 2, MidpointRounding.ToNegativeInfinity);//有乘法之后去小数点
            var dueAmout = Commission + vat;//有乘法之后去小数点
            if (bookingDetail.RestaurantIncluedVAT)
                _amount = dueAmout - reward;
            else if (reward > 0)
                _amount = dueAmout - reward;
            else
                _amount = dueAmout - reward - vat;

            ItemPayInfo itemPayInfo = new ItemPayInfo()
            {
                PayAmount = Math.Round(_amount, 2, MidpointRounding.ToPositiveInfinity),//VAT,有乘法之后去小数点
                Reward = reward,
                Vat = vat,
                Commission = Commission
            };
            return itemPayInfo;
        }
        /// <summary>
        /// 计算VAT
        /// </summary>
        /// <param name="payAmount"></param>
        /// <param name="VAT"></param>
        /// <returns></returns>
        private decimal GetVATAmount(decimal payAmount, double VAT)
        {
            return payAmount * (decimal)VAT;
        }
        private decimal GetDue(BookingCalculateVO bookingDetail, decimal amount)
        {
            switch (bookingDetail.BillInfo.PaymentType)
            {
                case PaymentTypeEnum.Full:
                    return amount;
                case PaymentTypeEnum.Percentage:
                    amount = amount * (decimal)bookingDetail.BillInfo.PayRate;
                    break;
                case PaymentTypeEnum.Fixed:
                    amount = (decimal)bookingDetail.BillInfo.PayRate * 100;
                    break;
                default:
                    break;
            }
            return amount;
        }
        /// <summary>
        /// 计算总金额
        /// </summary>
        /// <param name="bookingDetail"></param>
        /// <returns></returns>
        public decimal getItemAmount(BookingCalculateVO bookingDetail)
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
