using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Upland.Infrastructure.LocalData;
using Upland.Infrastructure.UplandApi;
using Upland.Types;

namespace Upland.InformationProcessor
{
    public class InformationProcessor
    {
        private LocalDataManager localDataManager;
        private UplandApiRepository uplandApi;

        public InformationProcessor()
        {
            localDataManager = new LocalDataManager();
            uplandApi = new UplandApiRepository();
        }

        public List<string> GetCollectionInformation()
        {
            List<string> output = new List<string>();

            List<Collection> collections = localDataManager.GetCollections();

            collections = collections.OrderBy(c => c.Category).OrderBy(c => c.CityId).ToList();

            output.Add("      Id - Category   - Name - Boost - Slots - Reward - Number of Properties");
            output.Add("");
            output.Add("Standard Collections");

            int maxNameLenght = collections.OrderByDescending(c => c.Name.Length).First().Name.Length;

            int? cityId = -1;
            foreach (Collection collection in collections)
            {
                if (cityId != collection.CityId)
                {
                    cityId = collection.CityId;
                    output.Add("");
                    output.Add(Consts.Cities[cityId.Value]);
                }

                output.Add(string.Format("     {0} - {1} - {2} - {3:N2} - {4} - {5} - {6}"
                    , collection.Id.ToString().PadLeft(3)
                    , HelperFunctions.GetCollectionCategory(collection.Category).PadRight(10)
                    , collection.Name.PadRight(maxNameLenght)
                    , collection.Boost
                    , collection.NumberOfProperties
                    , string.Format("{0:N0}", collection.Reward).ToString().PadLeft(7)
                    , string.Format("{0:N0}", collection.MatchingPropertyIds.Count).ToString().PadLeft(6)
                ));
            }

            return output;
        }

        public async Task<List<string>> GetMyPropertyInfo(string username)
        {
            List<string> output = new List<string>();
            List<Property> properties = await localDataManager.GetPropertysByUsername(username);

            properties = properties.OrderBy(p => p.Address).OrderBy(p => p.CityId).ToList();

            output.Add("                 Id -   Size - Monthly Earnings - Address");

            int? cityId = -1;
            foreach (Property property in properties)
            {
                if (cityId != property.CityId)
                {
                    cityId = property.CityId;
                    output.Add("");
                    output.Add(Consts.Cities[cityId.Value]);
                }

                output.Add(string.Format("     {0} - {1} - {2:} - {3}"
                    , property.Id
                    , string.Format("{0:N0}", property.Size).ToString().PadLeft(6)
                    , string.Format("{0:N2}", property.MonthlyEarnings).ToString().PadLeft(10)
                    , property.Address
                ));
            }

            return output;
        }
    }
}
