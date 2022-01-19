using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Upland.Interfaces.Managers;
using Upland.Interfaces.Processors;
using Upland.Types;
using Upland.Types.Types;

namespace Upland.InformationProcessor
{
    public class WebProcessor : IWebProcessor
    {
        private readonly ILocalDataManager _localDataManager;
        private readonly IUplandApiManager _uplandApiManager;

        public WebProcessor(ILocalDataManager localDataManager, IUplandApiManager uplandApiManager)
        {
            _localDataManager = localDataManager;
            _uplandApiManager = uplandApiManager;
        }

        public async Task<UserProfile> GetWebUIProfile(string uplandUsername)
        {
            UserProfile profile = await _uplandApiManager.GetUserProfile(uplandUsername);

            profile.Rank = HelperFunctions.TranslateUserLevel(int.Parse(profile.Rank));
            profile.EOSAccount = _localDataManager.GetEOSAccountByUplandUsername(uplandUsername);

            Dictionary<long, Property> userProperties = _localDataManager
                .GetProperties(profile.Properties.Select(p => p.PropertyId).ToList())
                .ToDictionary(p => p.Id, p => p);

            Dictionary<long, string> userBuildings = _localDataManager.GetPropertyStructures()
                .ToDictionary(p => p.PropertyId, p => p.StructureType);

            Dictionary<int, string> neighborhoods = _localDataManager.GetNeighborhoods()
                .ToDictionary(n => n.Id, n => n.Name);

            RegisteredUser registeredUser = _localDataManager.GetRegisteredUserByUplandUsername(uplandUsername);

            if (registeredUser != null)
            {
                profile.RegisteredUser = true;
                profile.RunCount = registeredUser.RunCount;
                profile.MaxRuns = Consts.FreeRuns + Convert.ToInt32(Math.Floor((double)(registeredUser.SendUPX / Consts.UPXPricePerRun)));
                profile.UPXToSupporter = Consts.SendUpxSupporterThreshold - registeredUser.SendUPX;
                profile.UPXToNextRun = Consts.UPXPricePerRun - registeredUser.SendUPX % Consts.UPXPricePerRun;
            }
            else
            {
                profile.RegisteredUser = false;
            }

            foreach (UserProfileProperty property in profile.Properties)
            {
                property.Address = userProperties[property.PropertyId].Address;
                property.City = Consts.Cities[userProperties[property.PropertyId].CityId];
                if (userProperties[property.PropertyId].NeighborhoodId == null)
                {
                    property.Neighborhood = "Unknown";
                }
                else
                {
                    property.Neighborhood = neighborhoods[userProperties[property.PropertyId].NeighborhoodId.Value];
                }
                property.Size = userProperties[property.PropertyId].Size;
                property.Mint = userProperties[property.PropertyId].Mint;
                property.Status = userProperties[property.PropertyId].Status;
                if (!userBuildings.ContainsKey(property.PropertyId))
                {
                    property.Building = "";
                }
                else
                {
                    property.Building = userBuildings[property.PropertyId];
                }
            }

            return profile;
        }
    }
}
