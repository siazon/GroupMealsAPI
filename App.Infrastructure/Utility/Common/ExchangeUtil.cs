using App.Domain.Common;
using App.Domain.Common.Shop;
using App.Domain.TravelMeals.Restaurant;
using App.Infrastructure.Exceptions;
using App.Infrastructure.Repository;
using Microsoft.AspNetCore.Http.Metadata;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog.LayoutRenderers.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace App.Infrastructure.Utility.Common
{
    public interface IExchangeUtil
    {
        Task<ExchangeModel> getGBPExchangeRate();
        void UpdateExchangeRateToDB();
    }
    public class ExchangeUtil : IExchangeUtil
    {
        private readonly IHttpClientFactory _httpClientFactory;
        IDbCommonRepository<DbCountry> _countryRepository;

        public ExchangeUtil(IHttpClientFactory httpClientFactory, IDbCommonRepository<DbCountry> countryRepository)
        {
            _httpClientFactory = httpClientFactory;
            _countryRepository = countryRepository;
        }

        public async Task<ExchangeModel> getGBPExchangeRate()
        {
            var httpClient = _httpClientFactory.CreateClient();
            var httpResponseMessage = await httpClient.GetAsync(
                "https://v6.exchangerate-api.com/v6/945925974267e80c40e247cd/latest/EUR");
            if (httpResponseMessage.IsSuccessStatusCode)
            {
                var jsonResponse = await httpResponseMessage.Content.ReadAsStringAsync();
                try
                {
                    var str = jsonResponse.Length;
                    var j = JsonConvert.DeserializeObject<ExchangeModel>(jsonResponse);
                    return j;
                }
                catch (Exception ex)
                {
                    Console.Write(ex.Message);
                }
            }
            return null;
        }
        public async void UpdateExchangeRateToDB()
        {
            var existRate = await _countryRepository.GetOneAsync(r => r.ShopId == 11);
            if (existRate != null)
            {
                if ((DateTime.UtcNow - existRate.RateUpdateTime).TotalHours < 23)
                    return;
                ExchangeModel exchange = await getGBPExchangeRate();
                if (exchange == null) return;
                foreach (var item in existRate.Countries)
                {
                    string sValue = "0";
                    double rate = 1;
                    switch (item.Currency)
                    {
                        case "UK":
                            sValue = exchange.conversion_rates["GBP"];
                            double.TryParse(sValue, out rate);
                            item.ExchangeRate = rate+item.ExchangeRateExtra;
                            break;
                        case "EU":
                            item.ExchangeRate = rate + item.ExchangeRateExtra;
                            break;
                        default:
                            sValue = exchange.conversion_rates[item.Currency];
                            double.TryParse(sValue, out rate);
                            item.ExchangeRate = rate + item.ExchangeRateExtra;
                            break;
                    }
                }

                existRate.RateUpdateTime = DateTime.UtcNow;
                var savedShop = await _countryRepository.UpsertAsync(existRate);
            }
        }
    }

}
