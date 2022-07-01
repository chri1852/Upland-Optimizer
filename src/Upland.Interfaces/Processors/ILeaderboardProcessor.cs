using System;
using System.Collections.Generic;
using Upland.Types.Enums;
using Upland.Types.Types;

namespace Upland.Interfaces.Processors
{
    public interface ILeaderboardProcessor
    {
        List<LeaderboardListItem> GetLeaderboardByType(LeaderboardTypeEnum type, DateTime fromTime, string additionalInfo = null);
    }
}
