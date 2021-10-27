using EosSharp;
using EosSharp.Core;
using EosSharp.Core.Api.v1;
using EosSharp.Core.Providers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Upland.Types.BlockchainTypes;

namespace Upland.Infrastructure.Blockchain
{
    public class BlockchainRepository
    {
        Eos eos;

        public BlockchainRepository()
        {
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
    }
}
