using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Upland.Infrastructure.LocalData;
using Upland.Infrastructure.UplandApi;
using Upland.Types;
using Upland.Types.Types;
using Upland.Types.UplandApiTypes;

namespace Upland.InformationProcessor
{
    public class ProfileAppraiser
    {
        private Dictionary<Tuple<string, int, string>, PropertyAppraisalData> _previousSalesData;
        private Dictionary<Tuple<string, int, string>, PropertyAppraisalData> _currentFloorData;
        private Dictionary<string, double> _buildingData;
        private Dictionary<long, string> _propertyStructures;

        private DateTime _expirationDate;

        private LocalDataManager _localDataManager;
        private UplandApiManager _uplandApiManager;

        private List<Collection> _collections;

        public ProfileAppraiser(LocalDataManager localDataManager, UplandApiManager uplandApiManager)
        {
            _localDataManager = localDataManager;
            _uplandApiManager = uplandApiManager;

            _collections = _localDataManager.GetCollections();

            RefreshData();
        }

        public async Task<List<string>> RunAppraisal(string username, string fileType)
        {
            List<PropertyAppraisal> appraisals = await RunAppraisal(username);

            if (fileType.ToUpper() == "TXT")
            {
                return BuildAppraisalTxtStrings(appraisals, username);
            }
            else
            {
                return BuildAppraisalCsvStrings(appraisals, username);
            }
        }

        private async Task<List<PropertyAppraisal>> RunAppraisal(string username)
        {
            UplandUserProfile userProfile = await _uplandApiManager.GetUplandUserProfile(username.ToLower());
            List<Property> properties = _localDataManager.GetProperties(userProfile.propertyList);
            List<PropertyAppraisal> propertyAppraisals = new List<PropertyAppraisal>();

            RefreshData();

            foreach (Property property in properties)
            {
                PropertyAppraisal propertyAppraisal = new PropertyAppraisal();
                propertyAppraisal.Notes = new List<string>();
                propertyAppraisal.Property = property;

                List<decimal> salesValues = new List<decimal>();
                List<decimal> floorValues = new List<decimal>();

                int maxCollectionCategory = 0;
                double maxFloor = 0;
                double upperBound = propertyAppraisal.Property.Mint;
                bool isLargeProp = false;

                // City
                Tuple<string, int, string> cityTupleUPX = new Tuple<string, int, string>("CITY", property.CityId, "UPX");
                if (_currentFloorData.ContainsKey(cityTupleUPX)) { floorValues.Add(_currentFloorData[cityTupleUPX].Value); }

                // Street
                Tuple<string, int, string> streetTupleUPX = new Tuple<string, int, string>("STREET", property.StreetId, "UPX");

                if (_previousSalesData.ContainsKey(streetTupleUPX)) 
                { 
                    salesValues.Add(_previousSalesData[streetTupleUPX].Value * property.Size);
                    isLargeProp = isLargeProp ? isLargeProp : propertyAppraisal.Property.Size > _previousSalesData[streetTupleUPX].AverageSize * 10;
                }
                if (_currentFloorData.ContainsKey(streetTupleUPX)) { floorValues.Add(_currentFloorData[streetTupleUPX].Value); }

                // Neighborhood
                if (property.NeighborhoodId != null)
                {
                    Tuple<string, int, string> neighborhoodTupleUPX = new Tuple<string, int, string>("NEIGHBORHOOD", property.NeighborhoodId.Value, "UPX");

                    if (_previousSalesData.ContainsKey(neighborhoodTupleUPX)) 
                    { 
                        salesValues.Add(_previousSalesData[neighborhoodTupleUPX].Value * property.Size);
                        isLargeProp = isLargeProp ? isLargeProp : propertyAppraisal.Property.Size > _previousSalesData[neighborhoodTupleUPX].AverageSize * 10;
                    }
                    if (_currentFloorData.ContainsKey(neighborhoodTupleUPX)) { floorValues.Add(_currentFloorData[neighborhoodTupleUPX].Value); }
                }
                else
                {
                    propertyAppraisal.Notes.Add("Missing Neighborhood");
                }

                // Collection
                foreach (Collection collection in _collections.Where(c => c.MatchingPropertyIds.Contains(property.Id)))
                {
                    Tuple<string, int, string> collectionTupleUPX = new Tuple<string, int, string>("COLLECTION", collection.Id, "UPX");

                    if (_previousSalesData.ContainsKey(collectionTupleUPX)) 
                    { 
                        salesValues.Add(_previousSalesData[collectionTupleUPX].Value * property.Size);
                        isLargeProp = isLargeProp ? isLargeProp : propertyAppraisal.Property.Size > _previousSalesData[collectionTupleUPX].AverageSize * 10;
                    }
                    if (_currentFloorData.ContainsKey(collectionTupleUPX)) { floorValues.Add(_currentFloorData[collectionTupleUPX].Value); }

                    if (maxCollectionCategory < collection.Category) { maxCollectionCategory = collection.Category; }
                }

                // Handle Low Data
                if (salesValues.Count == 0 && floorValues.Count == 0)
                {
                    propertyAppraisal.UPX_Upper = propertyAppraisal.Property.Mint;
                    propertyAppraisal.UPX_Lower = 0;
                    propertyAppraisal.Notes.Add("No Data Found To Calculate Value");
                }
                else
                {
                    if (salesValues.Count == 0)
                    {
                        salesValues.Add((int)Math.Round(property.Mint));
                        propertyAppraisal.Notes.Add("No Sales Data");
                    }

                    if (floorValues.Count == 0)
                    {
                        salesValues.Add((int)Math.Round(property.Mint));
                        propertyAppraisal.Notes.Add("No Floor Data");
                    }
                }

                if (floorValues.Count > 0)
                {
                    maxFloor = (double)floorValues.Max(v => v);
                }

                if(floorValues.Count + salesValues.Count > 0)
                {
                    salesValues.AddRange(floorValues);
                    upperBound = (double)salesValues.GroupBy(v => v).Select(g => g.First()).OrderByDescending(v => v).First();
                }

                if (maxFloor > upperBound)
                {
                    propertyAppraisal.UPX_Upper = maxFloor;
                    propertyAppraisal.UPX_Lower = upperBound;
                }
                else if(upperBound > maxFloor)
                {
                    propertyAppraisal.UPX_Upper = upperBound;
                    propertyAppraisal.UPX_Lower = maxFloor;
                }
                else
                {
                    // max floor and upxValues[0] are equal, drop one and put the mint in
                    if (propertyAppraisal.Property.Mint >= maxFloor)
                    {
                        propertyAppraisal.UPX_Upper = propertyAppraisal.Property.Mint;
                        propertyAppraisal.UPX_Lower = maxFloor;
                    }
                    else
                    {
                        propertyAppraisal.UPX_Upper = maxFloor;
                        propertyAppraisal.UPX_Lower = maxFloor;
                    }
                }

                if (maxCollectionCategory == 5)
                {
                    propertyAppraisal.Notes.Add(string.Format("Ultra Rare"));
                }

                if (isLargeProp)
                {
                    propertyAppraisal.Notes.Add(string.Format("Large Property"));
                }

                if (_propertyStructures.ContainsKey(property.Id))
                {
                    propertyAppraisal.UPX_Lower += _buildingData[_propertyStructures[property.Id]];
                    propertyAppraisal.UPX_Upper += _buildingData[_propertyStructures[property.Id]];
                    propertyAppraisal.Notes.Add(string.Format("{0}", _propertyStructures[property.Id]));
                }

                propertyAppraisals.Add(propertyAppraisal);
            }

            return propertyAppraisals;
        }

        private List<string> BuildAppraisalCsvStrings(List<PropertyAppraisal> propertyAppraisals, string username)
        {
            List<string> output = new List<string>();

            output.Add(string.Format("Id,CityId,Address,Size,Mint,Lower UPX Value,Upper UPX Value,Note"));

            propertyAppraisals = propertyAppraisals.OrderByDescending(p => p.UPX_Upper).OrderBy(p => p.Property.CityId).ToList();

            foreach (PropertyAppraisal appraisal in propertyAppraisals)
            {
                output.Add(string.Format("{0},{1},{2},{3},{4},{5},{6},{7}"
                    , appraisal.Property.Id
                    , appraisal.Property.CityId
                    , appraisal.Property.Address
                    , appraisal.Property.Size
                    , appraisal.Property.Mint
                    , appraisal.UPX_Lower
                    , appraisal.UPX_Upper
                    , string.Join(" ", appraisal.Notes)
                ));
            }

            return output;
        }

        private List<string> BuildAppraisalTxtStrings(List<PropertyAppraisal> propertyAppraisals, string username)
        {
            List<string> output = new List<string>();

            int idPad = 19;
            int cityIdPad = 6;
            int addressPad = propertyAppraisals.Max(p => p.Property.Address.Length);
            int sizePad = 9;
            int mintPad = 15;
            int upperPad = 15;
            int lowerPad = 15;
            int notePad = propertyAppraisals.Max(p => string.Join(", ", p.Notes).Length) < 4 ? 4 : propertyAppraisals.Max(p => string.Join(", ", p.Notes).Length);

            output.Add(string.Format("Property Appraisal For {0} as of {1:MM/dd/yy H:mm:ss}", username.ToUpper(), DateTime.Now));
            output.Add("");
            output.Add(string.Format("{0} - {1} - {2} - {3} - {4} - {5} - {6} - {7}"
                , "Id".PadLeft(idPad)
                , "CityId".PadLeft(cityIdPad)
                , "Address".PadLeft(addressPad)
                , "Size".PadLeft(sizePad)
                , "Mint".PadLeft(mintPad)
                , "Lower UPX Value".PadLeft(lowerPad)
                , "Upper UPX Value".PadLeft(upperPad)
                , "Note".PadLeft(notePad)));

            int? cityId = -1;
            propertyAppraisals = propertyAppraisals.OrderByDescending(p => p.UPX_Upper).OrderBy(p => p.Property.CityId).ToList();

            foreach (PropertyAppraisal appraisal in propertyAppraisals)
            {
                if (cityId != appraisal.Property.CityId)
                {
                    cityId = appraisal.Property.CityId;
                    output.Add("");
                    output.Add(string.Format("{0} - {1:N2} -> {2:N2}"
                        , Consts.Cities[cityId.Value]
                        , propertyAppraisals.Where(p => p.Property.CityId == cityId).Sum(p => p.UPX_Lower)
                        , propertyAppraisals.Where(p => p.Property.CityId == cityId).Sum(p => p.UPX_Upper)
                        ));
                }

                output.Add(string.Format("{0} - {1} - {2} - {3} - {4} - {5} - {6} - {7}"
                    , appraisal.Property.Id.ToString().PadLeft(idPad)
                    , appraisal.Property.CityId.ToString().PadLeft(cityIdPad)
                    , appraisal.Property.Address.PadLeft(addressPad)
                    , string.Format("{0:N0}", appraisal.Property.Size).PadLeft(sizePad)
                    , string.Format("{0:N2}", appraisal.Property.Mint).PadLeft(mintPad)
                    , string.Format("{0:N}", appraisal.UPX_Lower).PadLeft(lowerPad)
                    , string.Format("{0:N}", appraisal.UPX_Upper).PadLeft(upperPad)
                    , string.Join(", ", appraisal.Notes).PadLeft(notePad)
                ));
            }

            output.Add("");
            output.Add(string.Format("Total - {0:N2} -> {1:N2}"
                , propertyAppraisals.Sum(p => p.UPX_Lower)
                , propertyAppraisals.Sum(p => p.UPX_Upper)
                ));

            return output;
        }

        private void RefreshData()
        {
            if (_expirationDate == null || _expirationDate < DateTime.Now)
            {
                _previousSalesData = _localDataManager.GetPreviousSalesAppraisalData()
                    .ToDictionary(d => new Tuple<string, int, string>(d.Type, d.Id, d.Currency), d => d);
                _currentFloorData = _localDataManager.GetCurrentFloorAppraisalData()
                    .ToDictionary(d => new Tuple<string, int, string>(d.Type, d.Id, d.Currency), d => d);
                _buildingData = _localDataManager.GetBuildingAppraisalData()
                    .ToDictionary(d => d.Item1, d => d.Item2);
                _propertyStructures = _localDataManager.GetPropertyStructures()
                    .ToDictionary(d => d.PropertyId, d => d.StructureType);

                _expirationDate = DateTime.Now.AddHours(24);
            }
        }
    }
}
