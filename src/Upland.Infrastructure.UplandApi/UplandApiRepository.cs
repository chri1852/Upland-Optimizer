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

        public async Task<List<UplandForSaleProp>> GetForSalePropsInArea(double north, double south, double east, double west)
        {
            UplandForSalePropWrapper props;
            string requestUri = @"https://api.upland.me/properties/list-view?";
            requestUri = string.Format("{0}north={1}&south={2}&east={3}&west={4}&offset=0&limit=20&sort=asc", requestUri, north, south, east, west);

            props = await CallApi<UplandForSalePropWrapper>(requestUri);

            return props.Properties;
        }

        public async Task<UplandStreet> GetStreetById(int streetId)
        {
            UplandStreet street;
            string requestUri = @"https://api.upland.me/street/" + streetId ;

            street = await CallApi<UplandStreet>(requestUri, true);

            return street;
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
            catch
            {
                return (T)Activator.CreateInstance(typeof(T));
            }
        }
    }
}
