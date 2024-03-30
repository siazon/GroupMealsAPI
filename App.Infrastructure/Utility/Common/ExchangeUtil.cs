using App.Domain.Common;
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
    public interface IExcahngeUtil
    {
        Task<double> getGBPExchangeRate();
    }
    public class ExchangeUtil : IExcahngeUtil
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ExchangeUtil(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
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
                }
                catch (Exception ex)
                {
                    Console.Write(ex.Message);
                }
            }
            return Rate;
        }
    }

}
