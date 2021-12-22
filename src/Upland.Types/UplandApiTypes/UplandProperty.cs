using System;
using System.Collections.Generic;
using System.Text;

namespace Upland.Types.UplandApiTypes
{
    public class UplandProperty
    {
        public long Prop_Id { get; set; }
        public string Full_Address { get; set; }
        public LocationInfo City { get; set; }
        public LocationInfo Street { get; set; }
        public MarketInfo on_market { get; set; }
        public double? Yield_Per_Hour { get; set; }
        public int? Area { get; set; }
        public decimal CenterLat { get; set; }
        public decimal CenterLng { get; set; }
        public string transaction_id { get; set; }
        public string last_transaction_id { get; set; }
        public string status { get; set; }
        public uplandLabels labels { get; set; }
        public string owner { get; set; }
        public string owner_username { get; set; }
        public UplandBuilding building { get; set; }
    }

    public class LocationInfo
    {
        public int Id { get; set; }
    }

    public class MarketInfo
    {
        public string token;
        public string fiat;
        public string currency;
    }

    public class uplandLabels
    {
        public bool fsa_allow { get; set; }
    }

    public class UplandBuilding
    {
        public string constructionStatus { get; set; }
        public int propModelID { get; set; }
        public int nftID { get; set; }
        public UplandBuildingConstruction construction { get; set; }
    }

    public class UplandBuildingConstruction
    {
        public double stackedSparks { get; set; }
        public int totalSparksRequired { get; set; }
        public double progressInSparks { get; set; }
        public DateTime startedAt { get; set; }
        public DateTime finishedAt { get; set; }
    }
}
