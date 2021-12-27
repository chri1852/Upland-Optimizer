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
        private Dictionary<Tuple<string, int, string>, decimal> _previousSalesData;
        private Dictionary<Tuple<string, int, string>, decimal> _currentFloorData;
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
                List<decimal> upxValues = new List<decimal>();
                decimal maxFloor = 0;
                int maxCollectionCategory = 0;

                // Street
                Tuple<string, int, string> streetTupleUPX = new Tuple<string, int, string>("STREET", property.StreetId, "UPX");

                if (_previousSalesData.ContainsKey(streetTupleUPX)) { upxValues.Add(_previousSalesData[streetTupleUPX] * property.Size); }
                if (_currentFloorData.ContainsKey(streetTupleUPX)) { upxValues.Add(_currentFloorData[streetTupleUPX]); }

                if (_currentFloorData.ContainsKey(streetTupleUPX) && _currentFloorData[streetTupleUPX] > maxFloor) { maxFloor = _currentFloorData[streetTupleUPX]; }

                // Neighborhood
                if (property.NeighborhoodId != null)
                {
                    Tuple<string, int, string> neighborhoodTupleUPX = new Tuple<string, int, string>("NEIGHBORHOOD", property.NeighborhoodId.Value, "UPX");

                    if (_previousSalesData.ContainsKey(neighborhoodTupleUPX)) { upxValues.Add(_previousSalesData[neighborhoodTupleUPX] * property.Size); }
                    if (_currentFloorData.ContainsKey(neighborhoodTupleUPX)) { upxValues.Add(_currentFloorData[neighborhoodTupleUPX]); }

                    if (_currentFloorData.ContainsKey(neighborhoodTupleUPX) && _currentFloorData[neighborhoodTupleUPX] > maxFloor) { maxFloor = _currentFloorData[neighborhoodTupleUPX]; }
                }
                else
                {
                    propertyAppraisal.Notes.Add("Missing Neighborhood");
                }

                // Collection
                foreach (Collection collection in _collections.Where(c => c.MatchingPropertyIds.Contains(property.Id)))
                {
                    Tuple<string, int, string> collectionTupleUPX = new Tuple<string, int, string>("COLLECTION", collection.Id, "UPX");

                    if (_previousSalesData.ContainsKey(collectionTupleUPX)) { upxValues.Add(_previousSalesData[collectionTupleUPX] * property.Size); }
                    if (_currentFloorData.ContainsKey(collectionTupleUPX)) { upxValues.Add(_currentFloorData[collectionTupleUPX]); }

                    if (_currentFloorData.ContainsKey(collectionTupleUPX) && _currentFloorData[collectionTupleUPX] > maxFloor) { maxFloor = _currentFloorData[collectionTupleUPX]; }

                    if (maxCollectionCategory < collection.Category) { maxCollectionCategory = collection.Category; }
                }

                if (upxValues.Count == 0)
                {
                    upxValues.Add((int)Math.Round(property.MonthlyEarnings * 12 / 0.1728));
                    propertyAppraisal.Notes.Add("Not Enough Sales Data");
                }

                upxValues = upxValues.GroupBy(v => v).Select(g => g.First()).OrderByDescending(v => v).ToList();

                if (maxFloor > upxValues[0])
                {
                    propertyAppraisal.UPX_Upper = (double)maxFloor;
                    propertyAppraisal.UPX_Lower = (double)upxValues[0];
                }
                else
                {
                    propertyAppraisal.UPX_Upper = (double)upxValues[0];
                    propertyAppraisal.UPX_Lower = (double)maxFloor;
                }

                if (maxCollectionCategory == 5)
                {
                    propertyAppraisal.Notes.Add(string.Format("Ultra Rare"));
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

        public List<string> BuildAppraisalCsvStrings(List<PropertyAppraisal> propertyAppraisals, string username)
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
                    , Math.Round(appraisal.Property.MonthlyEarnings * 12 / 0.1728)
                    , appraisal.UPX_Lower
                    , appraisal.UPX_Upper
                    , string.Join(" ", appraisal.Notes)
                ));
            }

            return output;
        }

        public List<string> BuildAppraisalTxtStrings(List<PropertyAppraisal> propertyAppraisals, string username)
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
                    , string.Format("{0:N2}", Math.Round(appraisal.Property.MonthlyEarnings * 12 / 0.1728)).ToString().PadLeft(mintPad)
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
                    .ToDictionary(d => new Tuple<string, int, string>(d.Type, d.Id, d.Currency), d => d.Value);
                _currentFloorData = _localDataManager.GetCurrentFloorAppraisalData()
                    .ToDictionary(d => new Tuple<string, int, string>(d.Type, d.Id, d.Currency), d => d.Value);
                _buildingData = _localDataManager.GetBuildingAppraisalData()
                    .ToDictionary(d => d.Item1, d => d.Item2);
                _propertyStructures = _localDataManager.GetPropertyStructures()
                    .ToDictionary(d => d.PropertyId, d => d.StructureType);

                _expirationDate = DateTime.Now.AddHours(24);
            }
        }
    }
}
