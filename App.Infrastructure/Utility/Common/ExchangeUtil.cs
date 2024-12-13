using App.Domain.Common;
using App.Domain.Common.Shop;
using App.Domain.TravelMeals.Restaurant;
using App.Infrastructure.Exceptions;
using App.Infrastructure.Repository;
using App.Infrastructure.ServiceHandler.Common;
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
using System.Threading;
using System.Threading.Tasks;

namespace App.Infrastructure.Utility.Common
{
    public interface IExchangeUtil
    {
        Task<ExchangeModel> getGBPExchangeRate(string baseCode);
        void UpdateExchangeRateToDB();
    }
    public class ExchangeUtil : IExchangeUtil
    {
        private readonly IHttpClientFactory _httpClientFactory;
        ICountryServiceHandler _countryServiceHandler;

        public ExchangeUtil(IHttpClientFactory httpClientFactory, ICountryServiceHandler countryRepository)
        {
            _httpClientFactory = httpClientFactory;
            _countryServiceHandler = countryRepository;
        }

        public async Task<ExchangeModel> getGBPExchangeRate(string baseCode)
        {
            var httpClient = _httpClientFactory.CreateClient();
            var httpResponseMessage = await httpClient.GetAsync(
                "https://v6.exchangerate-api.com/v6/945925974267e80c40e247cd/latest/" + baseCode);
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
            var existRate = await _countryServiceHandler.GetStripes();
            if (existRate != null)
            {
                var Rates = existRate.ToList();
                if (Rates[0].Updated == null)
                {
                    Rates[0].Updated = DateTime.UtcNow;
                    await _countryServiceHandler.UpsertStripe(Rates[0]);
                }
                if ((DateTime.UtcNow - Rates[0].Updated.Value).TotalHours < 72)
                    return;
                await Task.Run(async () =>
                   {
                       foreach (var item in existRate)
                       {
                           ExchangeModel exchange = await getGBPExchangeRate(item.Currency);
                           if (exchange == null) return;
                           item.ExchangeRate = exchange;
                           item.Updated = DateTime.UtcNow;
                           await _countryServiceHandler.UpsertStripe(item);
                           Thread.Sleep(60000);
                       }
                   });


            }
        }
    }

}
