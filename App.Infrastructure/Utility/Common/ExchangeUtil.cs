using App.Domain.Common;
using App.Domain.Common.Shop;
using App.Infrastructure.Exceptions;
using App.Infrastructure.Repository;
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
        Task<double> getGBPExchangeRate();
         void UpdateToDB(double rate);
    }
    public class ExchangeUtil : IExchangeUtil
    {
        private readonly IHttpClientFactory _httpClientFactory;
        IDbCommonRepository<DbShop> _shopRepository;

        public ExchangeUtil(IHttpClientFactory httpClientFactory, IDbCommonRepository<DbShop> shopRepository)
        {
            _httpClientFactory = httpClientFactory;
            _shopRepository = shopRepository;
        }

        public async Task<double> getGBPExchangeRate()
        {
            double Rate = 1;
            var httpClient = _httpClientFactory.CreateClient();
            var httpResponseMessage = await httpClient.GetAsync(
                "https://v6.exchangerate-api.com/v6/945925974267e80c40e247cd/latest/EUR");
            if (httpResponseMessage.IsSuccessStatusCode)
            {
                var jsonResponse = await httpResponseMessage.Content.ReadAsStringAsync();
                try
                {
                    var str = jsonResponse.Length;
                    var j=JsonConvert.DeserializeObject<ExchangeModel>(jsonResponse);
                    var tee = j.conversion_rates["GBP"];
                    double.TryParse(tee,out Rate);
                    UpdateToDB(Rate);
                }
                catch (Exception ex)
                {
                    Console.Write(ex.Message);
                }
            }
            return Rate;
        }
        public async void UpdateToDB(double rate)
        {
            var existRate = await _shopRepository.GetOneAsync(r => r.ShopId == 11);
            if (existRate != null)
            {
                existRate.Updated = DateTime.UtcNow;
                existRate.RateUpdate = DateTime.UtcNow;
                existRate.ExchangeRate = rate;
                var savedShop = await _shopRepository.UpsertAsync(existRate);
            }
        }
    }

}
