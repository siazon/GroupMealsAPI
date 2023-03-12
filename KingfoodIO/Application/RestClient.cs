using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace KingfoodIO.Application
{
    public class RestClient
    {
        private const string ClientTokenKey =
           "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJFeHBpcmVkVGltZSI6IjIxMTktMTAtMjhUMTM6MjI6NTguNjgxNDk4NFoiLCJSb2xlTGV2ZWwiOm51bGwsIlVzZXJJZCI6bnVsbCwiVXNlck5hbWUiOm51bGwsIlNlcnZlcktleSI6IjczNDlDMzU5LTg5OUMtNENDQi1BQkY3LTY1QzkyN0ZGMTExMiIsIlNob3BLZXkiOiJEMzhEOUIiLCJpZCI6bnVsbCwiR3VpZCI6bnVsbCwiSGFzaCI6bnVsbCwiU2hvcElkIjowLCJDcmVhdGVkIjpudWxsLCJVcGRhdGVkIjpudWxsLCJSZXN1bHRzIjpmYWxzZX0._rKc8d15y0D15USpJEZ4xtyQC0FK4j0bu_rxKEjNG1A";

        private const string HeaderKey = "MMAuthToken";

        public async Task<HttpResponseMessage> GetService(string url)
        {
            var httpClient = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get, url);

            request.Headers.Add(HeaderKey, ClientTokenKey);

            return await httpClient.SendAsync(request);
        }

        public async Task<HttpResponseMessage> PostService(string url, HttpContent content)
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var request = new HttpRequestMessage(HttpMethod.Post, url);

            request.Headers.Add(HeaderKey, ClientTokenKey);

            request.Content = content;

            return await httpClient.SendAsync(request);
        }

        public async Task<HttpResponseMessage> PutService(string url, HttpContent content)
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var request = new HttpRequestMessage(HttpMethod.Put, url);

            request.Headers.Add(HeaderKey, ClientTokenKey);

            request.Content = content;

            return await httpClient.SendAsync(request);
        }

        public async Task<HttpResponseMessage> DeleteService(string url)
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var request = new HttpRequestMessage(HttpMethod.Delete, url);

            request.Headers.Add(HeaderKey, ClientTokenKey);

            return await httpClient.SendAsync(request);
        }
    }
}