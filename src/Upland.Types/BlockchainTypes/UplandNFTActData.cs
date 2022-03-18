using System.Collections.Generic;

namespace Upland.Types.BlockchainTypes
{
    public class UplandNFTActData
    {
        // Issue
        public List<int> dGood_Ids { get; set; }
        public string to { get; set; }
        public string category { get; set; }
        public string token_name { get; set; }
        public string quantity { get; set; }
        public string relative_uri { get; set; }
        public string memo { get; set; }

        // Burnnft
        public string owner { get; set; }

        // Create
        public string issuer { get; set; }
        public string rev_partner { get; set; }
        public bool fungible { get; set; }
        public bool burnable { get; set; }
        public bool sellable { get; set; }
        public bool transferable { get; set; }
        public bool sn_enabled { get; set; }
        public string rev_split { get; set; }
        public string base_uri { get; set; }
        public int max_issue_days { get; set; }
        public string max_supply { get; set; }
        public string display_name { get; set; }
        public int? series_id { get; set; }

        // Transfernft
        public string from { get; set; }

    }
}
