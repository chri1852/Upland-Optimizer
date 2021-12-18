using EosSharp;
using EosSharp.Core;
using EosSharp.Core.Api.v1;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Upland.Types.BlockchainTypes;

namespace Upland.Infrastructure.Blockchain
{
    public class BlockchainRepository
    {
        HttpClient httpClient;
        Eos eos;

        public BlockchainRepository()
        {
            this.httpClient = new HttpClient();
            this.httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            this.httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");

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

        public async Task<GetTransactionEntry> GetSingleTransactionById(string transactionId)
        {
            GetTransactionEntry transactionEntry;

            string requestUri = @"https://eos.greymass.com/v1/history/get_transaction?id=";
            requestUri += string.Format("{0}&before=", transactionId);

            transactionEntry = await CallApi<GetTransactionEntry>(requestUri);

            return transactionEntry;
        }

        public async Task<HistoryV2Query> GetPropertyActionsFromDateTime(DateTime fromTime, int minutesToAdd)
        {
            HistoryV2Query historyQuery;

            string requestUri = @"https://eos.hyperion.eosrio.io/v2/history/get_actions?account=playuplandme&filter=*%3An12,*%3An13,*%3An5,*%3An2,*%3An4,*%3An52,*%3Aa4,*%3An34,*%3An33&skip=0&limit=1000&sort=asc&after=";
            requestUri += string.Format("{0}&before=", fromTime.ToString("yyyy-MM-ddTHH:mm:ss"));
            requestUri += string.Format("{0}", fromTime.AddMinutes(minutesToAdd).ToString("yyyy-MM-ddTHH:mm:ss"));

            historyQuery = await CallApi<HistoryV2Query>(requestUri);

            return historyQuery;
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
