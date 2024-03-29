﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Upland.Types.Types;
using Upland.Types.UplandApiTypes;

namespace Upland.Interfaces.Managers
{
    public interface IUplandApiManager
    {
        Task<List<UplandForSaleProp>> GetForSalePropsByCityId(int cityId);
        string GetCacheDateTime(int cityId);
        void ClearSalesCache();
        Task<List<NFLPALegit>> GetNFLPALegitsByUsername(string userName);
        Task<List<SpiritLegit>> GetSpiritLegitsByUsername(string userName);
        Task<List<Decoration>> GetDecorationsByUsername(string userName);
        Task<List<BlockExplorer>> GetBlockExplorersByUserName(string userName);
        Task<NFLPALegit> GetNFLPALegitsByDGoodId(int dGoodId);
        Task<NFLPALegit> GetFootballLegitsByDGoodId(int dGoodId);
        Task<SpiritLegit> GetSpiritLegitsByDGoodId(int dGoodId);
        Task<Decoration> GetDecorationsByDGoodId(int dGoodId);
        Task<BlockExplorer> GetBlockExplorersByDGoodId(int dGoodId);
        Task<LandVehicle> GetLandVehicleByDGoodId(int dGoodId);
        Task<NFLPALegitMintInfo> GetEssentialMintInfo(int legitId);
        Task<NFLPALegitMintInfo> GetMementoMintInfo(int legitId);
        Task<LandVehicleFinishInfo> GetLandVehicleFinishInfo(int finishId);
        Task<UplandUserProfile> GetUplandUserProfile(string userName);
        Task<UserProfile> GetUserProfile(string userName);
        Task<UplandProperty> GetUplandPropertyById(long Id);
        Task<UplandExplorerCoordinates> GetExplorerCoordinates(string authToken = null);
        Task<UplandTreasureDirection> GetUplandTreasureDirection(long propId, string authToken = null);
        Task<bool> GetIsInMaintenance();
    }
}
        