using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Upland.Types.UplandApiTypes;

namespace Upland.Infrastructure.UplandApi
{
    public class UplandApiManager
    {
        private Dictionary<int, Tuple<DateTime, List<UplandForSaleProp>>> _saleCache;
        private UplandApiRepository _uplandApiRepository;
        private readonly int cacheTime = 15;

        public UplandApiManager()
        {
            _saleCache = new Dictionary<int, Tuple<DateTime, List<UplandForSaleProp>>>();
            _uplandApiRepository = new UplandApiRepository();
        } 

        public async Task<List<UplandForSaleProp>> GetForSalePropsByCityId(int cityId)
        {
            if (!_saleCache.ContainsKey(cityId))
            {
                await RefreshCache(cityId);
            }
            else if (_saleCache.ContainsKey(cityId) && _saleCache[cityId].Item1 < DateTime.Now)
            {
                _saleCache.Remove(cityId);
                await RefreshCache(cityId);
            }

            List<UplandForSaleProp> returnList = new List<UplandForSaleProp>();

            foreach (UplandForSaleProp prop in _saleCache[cityId].Item2)
            {
                returnList.Add(prop.Clone());
            }

            return returnList;
        }

        public string GetCacheDateTime(int cityId)
        {
            return string.Format("{0:MM/dd/yyy HH:mm:ss}", _saleCache[cityId].Item1);
        }

        public void ClearSalesCache()
        {
            _saleCache = new Dictionary<int, Tuple<DateTime, List<UplandForSaleProp>>>();
        }

        private async Task RefreshCache(int cityId)
        {
            List<UplandForSaleProp> cityProps = await CallApiForSalePropsByArea(cityId);
            _saleCache.Add(cityId, new Tuple<DateTime, List<UplandForSaleProp>>(DateTime.Now.AddMinutes(cacheTime), cityProps));
        }

        private async Task<List<UplandForSaleProp>> CallApiForSalePropsByArea(int cityId)
        {
            switch(cityId)
            {
                case 1:
                    return await _uplandApiRepository.GetForSalePropsInArea(37.828621064959904, 37.68835114990779, -122.33772472323983, -122.67472342810042);
                case 3:
                    return await _uplandApiRepository.GetForSalePropsInArea(40.897935779273865, 40.69910643047436, -73.5548670719332, -74.05386282956619);
                case 5:
                    return await _uplandApiRepository.GetForSalePropsInArea(36.94213849076949, 36.63772948290814, -119.3378952251502, -120.06021751017929);
                case 6:
                    return await _uplandApiRepository.GetForSalePropsInArea(40.73944377659254, 40.5401402329627, -73.56278241762573, -74.06177817525894);
                case 7:
                    return await _uplandApiRepository.GetForSalePropsInArea(37.890390577641824, 37.70070322240976, -121.90067808969837, -122.35672991708705);
                case 8:
                    return await _uplandApiRepository.GetForSalePropsInArea(40.650680936161564, 40.488905701824734, -74.0398230918665, -74.4443787554576);
                case 9:
                    return await _uplandApiRepository.GetForSalePropsInArea(35.478016363918215, 35.24329418643363, -118.72327963318267, -119.27015379486721);
                case 10:
                    return await _uplandApiRepository.GetForSalePropsInArea(42.11264759895798, 41.58238862582928, -87.0736963824263, -88.43397342182176);
                case 11:
                    return await _uplandApiRepository.GetForSalePropsInArea(41.609782691584314, 41.387344856244056, -81.35990844811214, -81.9241803054757);
                case 12:
                    return await _uplandApiRepository.GetForSalePropsInArea(37.46951308231604, 37.27710591853466, -121.716739599238, -122.17922994376318);
                case 13:
                    return await _uplandApiRepository.GetForSalePropsInArea(40.85554504499757, 40.78575359038544, -74.02452815445089, -74.19954351904273);
                case 14:
                    return await _uplandApiRepository.GetForSalePropsInArea(39.38557959467431, 38.75238588504746, -93.56859327586292, -95.11870645219331);
                case 15:
                    return await _uplandApiRepository.GetForSalePropsInArea(30.46192714455018, 29.580268899360252, -88.97599509417788, -90.91151219715452);

            }

            return new List<UplandForSaleProp>();
        }
    }
}
