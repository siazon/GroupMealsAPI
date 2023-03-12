using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace NewFoodCreator.Utility
{
    public class RestClient
    {

        private const string ClientTokenKey =
            "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJFeHBpcmVkVGltZSI6IlwvRGF0ZSg0Njc3MTcxMjk1MzIzKVwvIiwiUm9sZUxldmVsIjoxLCJVc2VySWQiOm51bGwsIlVzZXJOYW1lIjpudWxsLCJTZXJ2ZXJLZXkiOiJJbmRhU29mdFRha2Vhd2F5U29sdXRpb24iLCJJZCI6MCwiU2hvcElkIjowLCJDdXN0b21lcklkIjpudWxsLCJIYXNoIjpudWxsLCJHdWlkIjpudWxsLCJDcmVhdGVkIjpudWxsLCJVcGRhdGVkIjpudWxsLCJFcnJvck1lc3NhZ2UiOm51bGx9.8Bb3Cxxh4YFh66a9x6yA-iNEB6u2mPIWjPbSVSJezks";

        private const string HeaderKey = "IndaSoftAuthToken";

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