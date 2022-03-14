using EosSharp;
using EosSharp.Core;
using EosSharp.Core.Api.v1;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Upland.Interfaces.Repositories;
using Upland.Types.BlockchainTypes;

namespace Upland.Infrastructure.Blockchain
{
    public class BlockchainRepository : IBlockChainRepository
    {
        HttpClient httpClient;
        Eos eos;
        HttpClient eosFlareClient;

        public BlockchainRepository()
        {
            this.httpClient = new HttpClient();
            this.httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            this.httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");

            this.eosFlareClient = new HttpClient();
            this.eosFlareClient.DefaultRequestHeaders.Add("Accept", "application/json");
            this.eosFlareClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");

            this.eos = new Eos(new EosConfigurator()
            {
                HttpEndpoint = " https://eos.greymass.com", //Mainnet
                ExpireSeconds = 60,
            });
        }

        public async Task<List<dGood>> GetAllNFTs()
        {
            List<dGood> totalResults = new List<dGood>();
            GetTableRowsResponse result = new GetTableRowsResponse { more = true };
            int index = 0;

            while (result.more)
            {
                result = await this.eos.GetTableRows(new GetTableRowsRequest()
                {
                    json = true,
                    code = "uplandnftact",
                    scope = "uplandnftact",
                    table = "dgood",
                    lower_bound = index.ToString(),
                    upper_bound = null,
                    index_position = "0",
                    key_type = "",
                    limit = 5000,
                    reverse = false,
                    show_payer = false,
                });

                foreach (Newtonsoft.Json.Linq.JObject item in result.rows)
                {
                    totalResults.Add(item.ToObject<dGood>());
                }
                index = totalResults.Max(i => i.id);
            }

            // DEBUG GETS THE UNIQUE STRUCTURE NAMES
            //List<string> DEBUGSTRING = totalResults.Where(r => r.category == "structure").GroupBy(r => r.token_name).Select(g => g.First().token_name).ToList();

            return totalResults;
        }

        public async Task<List<a24Entry>> GetSparkStakingTable()
        {
            List<a24Entry> totalResults = new List<a24Entry>();
            GetTableRowsResponse result = new GetTableRowsResponse { more = true };
            int index = 0;

            while (result.more)
            {
                result = await this.eos.GetTableRows(new GetTableRowsRequest()
                {
                    json = true,
                    code = "playuplandme",
                    scope = "playuplandme",
                    table = "a24",
                    lower_bound = index.ToString(),
                    upper_bound = null,
                    index_position = "0",
                    key_type = "",
                    limit = 5000,
                    reverse = false,
                    show_payer = false,
                });

                foreach (Newtonsoft.Json.Linq.JObject item in result.rows)
                {
                    totalResults.Add(item.ToObject<a24Entry>());
                }
                index = totalResults.Max(i => i.f35);
            }

            return totalResults;
        }

        public async Task<List<a21Entry>> GetNftsRelatedToPropertys()
        {
            List<a21Entry> totalResults = new List<a21Entry>();
            GetTableRowsResponse result = new GetTableRowsResponse { more = true };
            int index = 0;

            while (result.more)
            {
                result = await this.eos.GetTableRows(new GetTableRowsRequest()
                {
                    json = true,
                    code = "playuplandme",
                    scope = "playuplandme",
                    table = "a21",
                    lower_bound = index.ToString(),
                    upper_bound = null,
                    index_position = "0",
                    key_type = "",
                    limit = 5000,
                    reverse = false,
                    show_payer = false,
                });

                foreach (Newtonsoft.Json.Linq.JObject item in result.rows)
                {
                    totalResults.Add(item.ToObject<a21Entry>());
                }
                index = totalResults.Max(i => i.f45);
            }

            return totalResults;
        }

        public async Task<List<t2Entry>> GetForSaleProps()
        {
            List<t2Entry> totalResults = new List<t2Entry>();
            GetTableRowsResponse result = new GetTableRowsResponse { more = true };
            long index = 0;

            while (result.more)
            {
                result = await this.eos.GetTableRows(new GetTableRowsRequest()
                {
                    json = true,
                    code = "playuplandme",
                    scope = "playuplandme",
                    table = "t2",
                    lower_bound = index.ToString(),
                    upper_bound = null,
                    index_position = "0",
                    key_type = "",
                    limit = 5000,
                    reverse = false,
                    show_payer = false,
                });

                foreach (Newtonsoft.Json.Linq.JObject item in result.rows)
                {
                    totalResults.Add(item.ToObject<t2Entry>());
                }
                index = totalResults.Max(i => i.a34);
            }

            return totalResults;
        }

        public async Task<List<t3Entry>> GetActiveOffers()
        {
            List<t3Entry> totalResults = new List<t3Entry>();
            GetTableRowsResponse result = new GetTableRowsResponse { more = true };
            long index = 0;

            while (result.more)
            {
                result = await this.eos.GetTableRows(new GetTableRowsRequest()
                {
                    json = true,
                    code = "playuplandme",
                    scope = "playuplandme",
                    table = "t3",
                    lower_bound = index.ToString(),
                    upper_bound = null,
                    index_position = "0",
                    key_type = "",
                    limit = 5000,
                    reverse = false,
                    show_payer = false,
                });

                foreach (Newtonsoft.Json.Linq.JObject item in result.rows)
                {
                    totalResults.Add(item.ToObject<t3Entry>());
                }
                index = totalResults.Max(i => i.f15);
            }

            return totalResults;
        }

        public async Task<T> GetSingleTransactionById<T>(string transactionId)
        {
            T transactionEntry;

            string requestUri = @"https://eos.greymass.com/v1/history/get_transaction?id=";
            requestUri += string.Format("{0}&before=", transactionId);

            transactionEntry = await CallApi<T>(requestUri);

            return transactionEntry;
        }

        public async Task<T> GetEOSFlareActions<T>(long position, string accountName)
        {
            string requestUri = @"https://api.eosflare.io/v1/eosflare/get_actions";

            HttpContent httpContent = new StringContent(
                JsonConvert.SerializeObject(new GetEOSFlareActionsRequest
                {
                    account_name = accountName,
                    pos = position,
                    offset = 1000
                }), 
                Encoding.UTF8, 
                "application/json");

            try
            {
                HttpResponseMessage httpResponse = await this.eosFlareClient.PostAsync(requestUri, httpContent);
                string responseJson = await httpResponse.Content.ReadAsStringAsync();

                return JsonConvert.DeserializeObject<T>(responseJson);
            }
            catch
            {
                return default(T);
            }
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
            catch
            {
                return (T)Activator.CreateInstance(typeof(T));
            }
        }
    }
}
