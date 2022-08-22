using BlockchainStoreApi.Messages;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Upland.BlockchainSurfer.BlockchainStoreApi
{
    public class BlockchainStoreApiRepository
    {
        private HttpClient httpClient;
        private readonly IConfiguration _configuration;

        private readonly string _applicationName;
        private readonly string _storeApiPassword;
        private string _blockchainStoreApiUrl;
        private bool _isAuthenticated;

        public BlockchainStoreApiRepository(IConfiguration configuration)
        {
            _configuration = configuration;
            this.httpClient = new HttpClient();
            this.httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            this.httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
            this.httpClient.Timeout = TimeSpan.FromSeconds(500);

            _applicationName = _configuration["AppSettings:ApplicationName"];
            _blockchainStoreApiUrl = _configuration["ConnectionStrings:OptimizerStoreApiUrl"];
            _storeApiPassword = _configuration["Credentials:StoreApiPassword"];
            _isAuthenticated = false;
        }

        public async Task<List<T>> GetActionsFromSequenceNumber<T>(string accountName, long sequenceNumber)
        {
            await CheckAuthentication();

            string requestUri = string.Format("{0}BlockchainDB/{1}/{2}", _blockchainStoreApiUrl, accountName, sequenceNumber);
            GetActionsResponse<T> response = await CallGetApi<GetActionsResponse<T>>(requestUri);

            return response.actions;
        }

        public async Task<List<T>> GetActionsByTransactionId<T>(string accountName, string transactionId)
        {
            await CheckAuthentication();

            string requestUri = string.Format("{0}BlockchainDB/{1}", _blockchainStoreApiUrl, accountName);
            HttpContent httpContent = new StringContent(
                JsonConvert.SerializeObject(new GetSingleActionRequest
                {
                    TransactionId = transactionId
                }),
                Encoding.UTF8,
                "application/json");

            try
            {
                HttpResponseMessage httpResponse = await this.httpClient.PostAsync(requestUri, httpContent);
                string responseJson = await httpResponse.Content.ReadAsStringAsync();

                GetActionsResponse<T> response = JsonConvert.DeserializeObject<GetActionsResponse<T>>(responseJson);
                return response.actions;
            }
            catch
            {
                throw new Exception("Failed Getting Single Transaction");
            }
        }

        private async Task CheckAuthentication()
        {
            if (_isAuthenticated)
            {
                return;
            }

            string requestUri = string.Format("{0}Authenticate/Login", _blockchainStoreApiUrl);
            HttpContent httpContent = new StringContent(
                JsonConvert.SerializeObject(new PostLoginRequest
                {
                    AccountName = _applicationName,
                    Password = _storeApiPassword
                }),
                Encoding.UTF8,
                "application/json");

            try
            {
                HttpResponseMessage httpResponse = await this.httpClient.PostAsync(requestUri, httpContent);
                string responseJson = await httpResponse.Content.ReadAsStringAsync();

                GetAuthenticationResponse response = JsonConvert.DeserializeObject<GetAuthenticationResponse>(responseJson);

                this.httpClient.DefaultRequestHeaders.Add("Authorization", response.AuthToken);
                _isAuthenticated = true;
            }
            catch
            {
                throw new Exception("Failed Authenticating");
            }
        }

        private async Task<T> CallGetApi<T>(string requestUri)
        {
            HttpResponseMessage httpResponse;
            string responseJson;

            httpResponse = await this.httpClient.GetAsync(requestUri);

            if (httpResponse.IsSuccessStatusCode)
            {
                try
                {
                    responseJson = await httpResponse.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<T>(responseJson);
                }
                catch
                {
                    throw new Exception("Failed Deserialization");
                }
            }
            else
            {
                throw new Exception(httpResponse.ReasonPhrase);
            }
        }
    }
}
