using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Upland.Types;
using Upland.Types.UplandApiTypes;

namespace Upland.Infrastructure.UplandApi
{
    public class UplandApiRepository
    {
        HttpClient httpClient;
        HttpClient authHttpClient;

        public UplandApiRepository()
        {
            this.httpClient = new HttpClient();
            this.httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            this.httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
            this.httpClient.DefaultRequestHeaders.Add("Host", "api.upland.me");

            this.authHttpClient = new HttpClient();
            this.authHttpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            this.authHttpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
            this.authHttpClient.DefaultRequestHeaders.Add("Host", "api.upland.me");
            this.authHttpClient.DefaultRequestHeaders.Add("Authorization", Consts.AuthToken);
        }

        public async Task<List<UplandCollection>> GetCollections()
        {
            List<UplandCollection> collections;
            string requestUri = @"https://api.upland.me/collections";

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

        public async Task<List<UplandNeighborhood>> GetNeighborhoods()
        {
            List<UplandNeighborhood> neighborhoods;
            string requestUri = @"https://api.upland.me/neighborhood";

            neighborhoods = await CallApi<List<UplandNeighborhood>>(requestUri);

            return neighborhoods;
        }

        public async Task<UplandStreet> GetStreetById(int streetId)
        {
            UplandStreet street;
            string requestUri = @"https://api.upland.me/street/" + streetId ;

            street = await CallApi<UplandStreet>(requestUri, true);

            return street;
        }

        public async Task<List<UplandProperty>> GetForSaleCollectionProperties(int collectionId)
        {
            List<UplandProperty> properties;
            string requestUri = @"https://api.upland.me/collections/match/reverse/unlocked/" + collectionId + "?limit=100000&offset=0";

            properties = await CallApi<List<UplandProperty>>(requestUri, true);

            return properties;
        }

        public async Task<List<UplandProperty>> GetUnlockedNotForSaleCollectionProperties(int collectionId)
        {
            List<UplandProperty> properties;
            string requestUri = @"https://api.upland.me/collections/match/reverse/owned/" + collectionId + "?limit=100000&offset=0";

            properties = await CallApi<List<UplandProperty>>(requestUri, true);

            return properties;
        }

        public async Task<List<UplandCollection>> GetMatchingCollectionsByPropertyId(long propertyId)
        {
            List<UplandCollection> collections;
            string requestUri = @"https://api.upland.me/properties/match/" + propertyId;

            collections = await CallApi<List<UplandCollection>>(requestUri, true);

            return collections;
        }

        public async Task<List<UplandProperty>> GetMatchingCollectionsOwned(int collectionId)
        {
            List<UplandProperty> properties;
            string requestUri = @"https://api.upland.me/collections/match/" + collectionId;

            properties = await CallApi<List<UplandProperty>>(requestUri, true);

            return properties;
        }

        public async Task<UplandDistinctProperty> GetPropertyById(long propertyId)
        {
            UplandDistinctProperty property;
            string requestUri = @"https://api.upland.me/properties/" + propertyId;

            property = await CallApi<UplandDistinctProperty>(requestUri);

            return property;
        }

        public async Task<List<UplandPropId>> GetPropertyIdsByUsername(string username)
        {
            List<UplandPropId> properties;
            string requestUri = @"https://api.upland.me/properties/list/" + username;

            properties = await CallApi<List<UplandPropId>>(requestUri, true);

            return properties;
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
