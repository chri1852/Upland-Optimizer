﻿using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Upland.Interfaces.Repositories;
using Upland.Types.Types;
using Upland.Types.UplandApiTypes;

namespace Upland.Infrastructure.UplandApi
{
    public class UplandApiRepository : IUplandApiRepository
    {
        private HttpClient httpClient;
        private HttpClient authHttpClient;
        private HttpClient changingAuthHttpClient;
        private readonly IConfiguration _configuration;

        public UplandApiRepository(IConfiguration configuration)
        {
            _configuration = configuration;
            this.httpClient = new HttpClient();
            this.httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            this.httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");

            this.authHttpClient = new HttpClient();
            this.authHttpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            this.authHttpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
            this.authHttpClient.DefaultRequestHeaders.Add("Authorization", _configuration.GetSection("AppSettings")["UplandAuthToken"]);

            this.changingAuthHttpClient = new HttpClient();
            this.changingAuthHttpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            this.changingAuthHttpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
            this.changingAuthHttpClient.DefaultRequestHeaders.Add("Authorization", _configuration.GetSection("AppSettings")["UplandAuthToken"]);
        }

        public async Task<List<UplandCollection>> GetCollections()
        {
            List<UplandCollection> collections;
            string requestUri = @"https://api.upland.me/collections/";

            collections = await CallApi<List<UplandCollection>>(requestUri);

            return collections;
        }

        public async Task<List<UplandCity>> GetCities()
        {
            List<UplandCity> cities;
            string requestUri = @"https://api.upland.me/city";

            cities = await CallApi<List<UplandCity>>(requestUri);

            return cities;
        }

        public async Task<List<Neighborhood>> GetNeighborhoods()
        {
            List<Neighborhood> neighborhoods;
            string requestUri = @"https://api.upland.me/neighborhood";

            neighborhoods = await CallApi<List<Neighborhood>>(requestUri);

            return neighborhoods;
        }

        public async Task<Street> GetStreet(int streetId)
        {
            Street street;
            string requestUri = @"https://api.upland.me/street";
            requestUri = string.Format("{0}/{1}", requestUri, streetId);

            street = await CallApi<Street>(requestUri, true);

            return street;
        }

        public async Task<List<UplandForSaleProp>> GetForSalePropsInArea(double north, double south, double east, double west)
        {
            UplandForSalePropWrapper props;
            string requestUri = @"https://api.upland.me/properties/list-view?";
            requestUri = string.Format("{0}north={1}&south={2}&east={3}&west={4}&offset=0&limit=20&sort=asc", requestUri, north, south, east, west);

            props = await CallApi<UplandForSalePropWrapper>(requestUri);

            return props.Properties;
        }

        public async Task<List<UplandAuthProperty>> GetForSaleCollectionProperties(int collectionId)
        {
            List<UplandAuthProperty> properties;
            string requestUri = @"https://api.upland.me/collections/match/reverse/unlocked/" + collectionId + "?limit=100000&offset=0";

            properties = await CallApi<List<UplandAuthProperty>>(requestUri, true);

            return properties;
        }

        public async Task<List<UplandAuthProperty>> GetUnlockedNotForSaleCollectionProperties(int collectionId)
        {
            List<UplandAuthProperty> properties;
            string requestUri = @"https://api.upland.me/collections/match/reverse/owned/" + collectionId + "?limit=100000&offset=0";

            properties = await CallApi<List<UplandAuthProperty>>(requestUri, true);

            return properties;
        }

        public async Task<List<UplandCollection>> GetMatchingCollectionsByPropertyId(long propertyId)
        {
            List<UplandCollection> collections;
            string requestUri = @"https://api.upland.me/properties/match/" + propertyId;

            collections = await CallApi<List<UplandCollection>>(requestUri, true);

            return collections;
        }

        public async Task<List<UplandAuthProperty>> GetMatchingCollectionsOwned(int collectionId)
        {
            List<UplandAuthProperty> properties;
            string requestUri = @"https://api.upland.me/collections/match/" + collectionId;

            properties = await CallApi<List<UplandAuthProperty>>(requestUri, true);

            return properties;
        }

        public async Task<UplandProperty> GetPropertyById(long propertyId)
        {
            UplandProperty property;
            string requestUri = @"https://api.upland.me/properties/" + propertyId;

            property = await CallApi<UplandProperty>(requestUri);

            return property;
        }

        public async Task<List<UplandAuthProperty>> GetPropertysByUsername(string username)
        {
            List<UplandAuthProperty> properties;
            string requestUri = @"https://api.upland.me/properties/list/" + username;

            properties = await CallApi<List<UplandAuthProperty>>(requestUri, true);

            return properties;
        }

        public async Task<List<UplandProperty>> GetPropertiesByArea(double north, double west, double defaultStep)
        {
            List<UplandProperty> properties;
            string requestUri = @"https://api.upland.me/map?north=" + north + "&south=" + (north - defaultStep) + "&east=" + (west + defaultStep) + "&west=" + west + "&marker=true";

            properties = await CallApi<List<UplandProperty>>(requestUri);

            return properties;
        }

        public async Task<List<UplandAsset>> GetNFLPALegitsByUserName(string username)
        {
            List<UplandAsset> assets;
            string requestUri = @"https://nft.upland.me/assets/legits/" + username;

            assets = await CallApi<List<UplandAsset>>(requestUri, false);

            return assets;
        }

        public async Task<List<UplandAsset>> GetSpiritLegitsByUserName(string username)
        {
            List<UplandAsset> assets;
            string requestUri = @"https://nft.upland.me/assets/spirits/" + username;

            assets = await CallApi<List<UplandAsset>>(requestUri, false);

            return assets;
        }

        public async Task<List<UplandAsset>> GetDecorationsByUserName(string username)
        {
            List<UplandAsset> assets;
            string requestUri = @"https://nft.upland.me/assets/decorations/" + username;

            assets = await CallApi<List<UplandAsset>>(requestUri, false);

            return assets;
        }

        public async Task<List<UplandAsset>> GetBlockExplorersByUserName(string username)
        {
            List<UplandAsset> assets;
            string requestUri = @"https://nft.upland.me/assets/block-explorers/" + username;

            assets = await CallApi<List<UplandAsset>>(requestUri, false);

            return assets;
        }

        public async Task<UplandAsset> GetNFLPALegitsByDGoodId(int dGoodId)
        {
            UplandAsset assets;
            string requestUri = @"https://nft.upland.me/assets/legits/nft-id/" + dGoodId;

            assets = await CallApi<UplandAsset>(requestUri, false);

            return assets;
        }

        public async Task<UplandAsset> GetFootballLegitsByDGoodId(int dGoodId)
        {
            UplandAsset assets;
            string requestUri = @"https://nft.upland.me/assets/football/nft-id/" + dGoodId;

            assets = await CallApi<UplandAsset>(requestUri, false);

            return assets;
        }

        public async Task<UplandAsset> GetSpiritLegitsByDGoodId(int dGoodId)
        {
            UplandAsset assets;
            string requestUri = @"https://nft.upland.me/assets/spirits/nft-id/" + dGoodId;

            assets = await CallApi<UplandAsset>(requestUri, false);

            return assets;
        }

        public async Task<UplandAsset> GetDecorationsByDGoodId(int dGoodId)
        {
            UplandAsset assets;
            string requestUri = @"https://nft.upland.me/assets/decorations/nft-id/" + dGoodId;

            assets = await CallApi<UplandAsset>(requestUri, false);

            return assets;
        }

        public async Task<UplandAsset> GetBlockExplorersByDGoodId(int dGoodId)
        {
            UplandAsset assets;
            string requestUri = @"https://nft.upland.me/assets/block-explorers/nft-id/" + dGoodId;

            assets = await CallApi<UplandAsset>(requestUri, false);

            return assets;
        }

        public async Task<UplandAsset> GetLandVehicleByDGoodId(int dGoodId)
        {
            UplandAsset assets;
            string requestUri = @"https://nft.upland.me/assets/cars/nft-id/" + dGoodId;

            assets = await CallApi<UplandAsset>(requestUri, false);

            return assets;
        }

        public async Task<NFLPALegitMintInfo> GetEssentialMintInfo(int legitId)
        {
            NFLPALegitMintInfo asset;
            string requestUri = @"https://nflpa.upland.me/legit-mint-info/essential/" + legitId;

            asset = await CallApi<NFLPALegitMintInfo>(requestUri, false);

            return asset;
        }

        public async Task<NFLPALegitMintInfo> GetMementoMintInfo(int legitId)
        {
            NFLPALegitMintInfo asset;
            string requestUri = @"https://nflpa.upland.me/legit-mint-info/memento/" + legitId;

            asset = await CallApi<NFLPALegitMintInfo>(requestUri, false);

            return asset;
        }

        public async Task<UplandLandVehicleFinishInfo> GetLandVehicleFinishInfo(int finishId)
        {
            UplandLandVehicleFinishInfo finishInfo;
            string requestUri = @"https://cars.upland.me/cars/" + finishId;

            finishInfo = await CallApi<UplandLandVehicleFinishInfo>(requestUri, false);

            finishInfo.FinishId = finishId;

            return finishInfo;
        }

        public async Task<UplandUserProfile> GetProfileByUsername(string username)
        {
            UplandUserProfile profile;
            string requestUri = @"https://api.upland.me/profile/" + username;

            profile = await CallApi<UplandUserProfile>(requestUri, true);

            return profile;
        }

        public async Task<UplandExplorerCoordinates> GetExplorerCoordinates(string authToken = null)
        {
            UplandExplorerCoordinates coordinates;
            string requestUri = @"https://explorers.upland.me/explorer/coordinates";

            if (authToken == null)
            {
                coordinates = await CallApi<UplandExplorerCoordinates>(requestUri, true);
            }
            else
            {
                coordinates = await CallApiChangingHeader<UplandExplorerCoordinates>(requestUri, authToken);
            }

            return coordinates;
        }

        public async Task<UplandTreasureDirection> GetUplandTreasureDirection(long propId, string authToken = null)
        {
            UplandTreasureDirection directions;

            string requestUri = @"https://treasures.upland.me/treasures/direction/" + propId;

            if (authToken == null)
            {
                directions = await CallApi<UplandTreasureDirection>(requestUri, true);
            }
            else
            {
                directions = await CallApiChangingHeader<UplandTreasureDirection>(requestUri, authToken);
            }

            return directions;
        }

        public async Task<bool> GetIsInMaintenance()
        {
            UplandMaintenance uplandMaintenance;
            string requestUri = @"https://api.upland.me/settings/maintenance";

            uplandMaintenance = await CallApi<UplandMaintenance>(requestUri, false);

            return uplandMaintenance.is_under_maintenance;
        }

        private async Task<T> CallApi<T>(string requestUri, bool useAuth = false)
        {
            HttpResponseMessage httpResponse;
            string responseJson;

            if (useAuth)
            {
                httpResponse = await this.authHttpClient.GetAsync(requestUri);
            }
            else
            {
                httpResponse = await this.httpClient.GetAsync(requestUri);
            }

            if (httpResponse.IsSuccessStatusCode)
            {
                responseJson = await httpResponse.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<T>(responseJson);
            }
            else if (httpResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return (T)Activator.CreateInstance(typeof(T));
            }
            else
            {
                return (T)Activator.CreateInstance(typeof(T));
            }    
        }

        private async Task<T> CallApiChangingHeader<T>(string requestUri, string authToken)
        {
            HttpResponseMessage httpResponse;
            string responseJson;

            this.changingAuthHttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);

            httpResponse = await this.changingAuthHttpClient.GetAsync(requestUri);


            if (httpResponse.IsSuccessStatusCode)
            {
                responseJson = await httpResponse.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<T>(responseJson);
            }
            else if (httpResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return (T)Activator.CreateInstance(typeof(T));
            }
            else
            {
                return (T)Activator.CreateInstance(typeof(T));
            }
        }
    }
}
