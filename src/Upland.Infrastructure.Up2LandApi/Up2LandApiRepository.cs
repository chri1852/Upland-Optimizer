using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Upland.Types;
using Upland.Types.Up2LandApiTypes;
using Upland.Types.UplandApiTypes;

namespace Upland.Infrastructure.Up2LandApi
{
    public class Up2LandApiRepository
    {
        HttpClient httpClient;

        public Up2LandApiRepository()
        {
            this.httpClient = new HttpClient();

            this.httpClient.DefaultRequestHeaders.Add("Origin", "https://upx.world");
            this.httpClient.DefaultRequestHeaders.Add("Referer", "https://upx.world/");
            this.httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
            this.httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            this.httpClient.DefaultRequestHeaders.Add("sec-ch-ua", "\" Not;A Brand\";v=\"99\", \"Google Chrome\";v=\"91\", \"Chromium\";v=\"91\"");
            this.httpClient.DefaultRequestHeaders.Add("sec-ch-ua-mobile", "?0");
            this.httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Site", "cross-site");
            this.httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "cors");
            this.httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "empty");
            this.httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
            this.httpClient.DefaultRequestHeaders.Add("Host", "api.up2land.com");
        }

        public async Task<List<Up2LandProperty>> GetUserProperties(string userName)
        {
            Up2landResponse<Up2LandUserData> userDataResponse;
            string requestUri = @"https://api.up2land.com/upland/" + userName;

            userDataResponse = await CallApi<Up2landResponse<Up2LandUserData>>(requestUri);

            return userDataResponse.Data.Properties;
        }

        private async Task<T> CallApi<T>(string requestUri)
        {
            HttpResponseMessage httpResponse;
            string responseJson;

            httpResponse = await this.httpClient.GetAsync(requestUri);
            responseJson = await httpResponse.Content.ReadAsStringAsync();

            try
            {
                return JsonConvert.DeserializeObject<T>(responseJson);
            }
            catch (Exception ex)
            {
                return (T)Activator.CreateInstance(typeof(T));
            }
        }
    }
}